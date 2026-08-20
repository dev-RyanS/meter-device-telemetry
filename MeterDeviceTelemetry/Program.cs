using MeterDeviceTelemetry.Application;
using MeterDeviceTelemetry.Contracts;
using MeterDeviceTelemetry.Configuration;
using MeterDeviceTelemetry.Data;
using MeterDeviceTelemetry.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("TelemetryDatabase")
    ?? "Data Source=telemetry.db";
builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseSqlite(connectionString));

builder.Services.AddOptions<TelemetryOptions>()
    .Bind(builder.Configuration.GetSection(TelemetryOptions.SectionName))
    .Validate(
        options => options.BatteryLowThreshold is >= 0 and <= 100,
        "Telemetry:BatteryLowThreshold must be between 0 and 100.")
    .ValidateOnStart();

builder.Services.AddScoped<MeterReadingService>(services =>
    new MeterReadingService(
        services.GetRequiredService<TelemetryDbContext>(),
        services.GetRequiredService<ILogger<MeterReadingService>>(),
        services.GetRequiredService<IOptions<TelemetryOptions>>().Value.BatteryLowThreshold));

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var database = scope.ServiceProvider.GetRequiredService<TelemetryDbContext>();
    await database.Database.EnsureCreatedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault();

    if (string.IsNullOrWhiteSpace(correlationId))
    {
        correlationId = context.TraceIdentifier;
    }

    context.Response.Headers["X-Correlation-ID"] = correlationId;

    using (app.Logger.BeginScope("CorrelationId: {CorrelationId}", correlationId))
    {
        await next();
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
    .WithName("Health")
    .WithOpenApi();

app.MapPost("/api/readings", async (
    MeterReadingRequest request,
    MeterReadingService readingService) =>
{
    var result = await readingService.CreateAsync(request);

    return result switch
    {
        MeterReadingCreationResult.Invalid invalid =>
            Results.BadRequest(new { errors = invalid.Errors }),
        MeterReadingCreationResult.Duplicate =>
            Results.Conflict(new
            {
                error = "A reading with this external ID already exists for this tenant."
            }),
        MeterReadingCreationResult.Created created =>
            Results.Created(
                "/api/readings",
                MeterReadingResponseMapper.Map(created.Reading, created.Status)),
        _ => throw new InvalidOperationException("Unknown meter reading creation result.")
    };
})
.WithName("CreateReading")
.WithOpenApi();

app.MapGet("/api/readings", async (
    string? tenantId,
    string? deviceId,
    string? type,
    DateTimeOffset? from,
    DateTimeOffset? to,
    int? page,
    int? pageSize,
    TelemetryDbContext database,
    ILogger<Program> logger,
    IOptions<TelemetryOptions> telemetryOptions) =>
{
    if (string.IsNullOrWhiteSpace(tenantId))
    {
        return Results.BadRequest(new { errors = new[] { "tenantId is required." } });
    }

    var requestedPage = page ?? 1;
    var requestedPageSize = pageSize ?? 20;

    if (requestedPage < 1)
    {
        return Results.BadRequest(new { errors = new[] { "page must be at least 1." } });
    }

    if (requestedPageSize is < 1 or > 100)
    {
        return Results.BadRequest(new { errors = new[] { "pageSize must be between 1 and 100." } });
    }

    if (from > to)
    {
        return Results.BadRequest(new { errors = new[] { "from must be earlier than or equal to to." } });
    }

    var batteryLowThreshold = telemetryOptions.Value.BatteryLowThreshold;

    var query = database.Readings
        .AsNoTracking()
        .Where(reading => reading.TenantId == tenantId);

    if (!string.IsNullOrWhiteSpace(deviceId))
    {
        query = query.Where(reading => reading.DeviceId == deviceId);
    }

    if (!string.IsNullOrWhiteSpace(type))
    {
        query = query.Where(reading => reading.Type == type);
    }

    if (from is not null)
    {
        query = query.Where(reading => reading.RecordedAt >= from.Value);
    }

    if (to is not null)
    {
        query = query.Where(reading => reading.RecordedAt <= to.Value);
    }

    var totalCount = await query.CountAsync();
    var readings = await query
        .OrderByDescending(reading => reading.RecordedAt)
        .Skip((requestedPage - 1) * requestedPageSize)
        .Take(requestedPageSize)
        .ToListAsync();
    var readingResults = readings
        .Select(reading => MeterReadingResponseMapper.Map(
            reading,
            MeterReadingStatusCalculator.Calculate(reading, batteryLowThreshold)))
        .ToList();

    logger.LogInformation(
        "Queried meter readings for tenant {TenantId}, returned {ReadingCount} of {TotalCount}",
        tenantId,
        readings.Count,
        totalCount);

    return Results.Ok(new PagedMeterReadingsResponse(
        readingResults,
        requestedPage,
        requestedPageSize,
        totalCount));
})
.WithName("GetReadings")
.WithOpenApi();

app.Run();

public partial class Program
{
}
