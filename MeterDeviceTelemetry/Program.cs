using MeterDeviceTelemetry.Contracts;
using MeterDeviceTelemetry.Data;
using MeterDeviceTelemetry.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = builder.Configuration.GetConnectionString("TelemetryDatabase")
    ?? "Data Source=telemetry.db";
builder.Services.AddDbContext<TelemetryDbContext>(options =>
    options.UseSqlite(connectionString));

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

var batteryLowThreshold = builder.Configuration.GetValue("Telemetry:BatteryLowThreshold", 20);

app.MapPost("/api/readings", async (
    MeterReadingRequest request,
    TelemetryDbContext database,
    ILogger<Program> logger) =>
{
    var missingFields = new List<string>();

    if (request.TenantId is null)
    {
        missingFields.Add("TenantId is required.");
    }

    if (request.DeviceId is null)
    {
        missingFields.Add("DeviceId is required.");
    }

    if (request.Type is null)
    {
        missingFields.Add("Type is required.");
    }

    if (request.Value is null)
    {
        missingFields.Add("Value is required.");
    }

    if (request.Unit is null)
    {
        missingFields.Add("Unit is required.");
    }

    if (request.Battery is null)
    {
        missingFields.Add("Battery is required.");
    }

    if (request.Signal is null)
    {
        missingFields.Add("Signal is required.");
    }

    if (request.RecordedAt is null)
    {
        missingFields.Add("RecordedAt is required.");
    }

    if (request.ExternalId is null)
    {
        missingFields.Add("ExternalId is required.");
    }

    if (missingFields.Count > 0)
    {
        return Results.BadRequest(new { errors = missingFields });
    }

    var reading = new MeterReading(
        request.TenantId!,
        request.DeviceId!,
        request.Type!,
        request.Value!.Value,
        request.Unit!,
        request.Battery!.Value,
        request.Signal!.Value,
        request.RecordedAt!.Value,
        request.ExternalId!);

    var validationErrors = MeterReadingValidator.Validate(reading);
    if (validationErrors.Count > 0)
    {
        return Results.BadRequest(new { errors = validationErrors });
    }

    var status = MeterReadingStatusCalculator.Calculate(reading, batteryLowThreshold);

    database.Readings.Add(reading);

    try
    {
        await database.SaveChangesAsync();
    }
    catch (DbUpdateException exception) when (
        exception.InnerException is SqliteException sqliteException &&
        sqliteException.SqliteErrorCode == 19)
    {
        logger.LogInformation(
            "Duplicate meter reading rejected for tenant {TenantId} and external ID {ExternalId}",
            reading.TenantId,
            reading.ExternalId);

        return Results.Conflict(new
        {
            error = "A reading with this external ID already exists for this tenant."
        });
    }

    logger.LogInformation(
        "Stored meter reading for tenant {TenantId}, device {DeviceId}, type {Type}",
        reading.TenantId,
        reading.DeviceId,
        reading.Type);

    return Results.Created("/api/readings", new { reading, status });
})
.WithName("CreateReading")
.WithOpenApi();

app.Run();
