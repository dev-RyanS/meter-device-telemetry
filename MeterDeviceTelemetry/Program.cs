using MeterDeviceTelemetry.Application;
using MeterDeviceTelemetry.Api;
using MeterDeviceTelemetry.Configuration;
using MeterDeviceTelemetry.Data;
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

app.MapReadingEndpoints();

app.Run();

public partial class Program
{
}
