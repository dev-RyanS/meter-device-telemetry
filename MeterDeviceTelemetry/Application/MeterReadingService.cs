using MeterDeviceTelemetry.Contracts;
using MeterDeviceTelemetry.Data;
using MeterDeviceTelemetry.Domain;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MeterDeviceTelemetry.Application;

public sealed class MeterReadingService
{
    private readonly TelemetryDbContext database;
    private readonly ILogger<MeterReadingService> logger;
    private readonly int batteryLowThreshold;

    public MeterReadingService(
        TelemetryDbContext database,
        ILogger<MeterReadingService> logger,
        int batteryLowThreshold)
    {
        this.database = database;
        this.logger = logger;
        this.batteryLowThreshold = batteryLowThreshold;
    }

    public async Task<MeterReadingCreationResult> CreateAsync(
        MeterReadingRequest request,
        CancellationToken cancellationToken = default)
    {
        var reading = new MeterReading(
            request.TenantId,
            request.DeviceId,
            request.Type,
            request.Value,
            request.Unit,
            request.Battery,
            request.Signal,
            request.RecordedAt,
            request.ExternalId);

        var validationErrors = MeterReadingValidator.Validate(reading);
        if (validationErrors.Count > 0)
        {
            return new MeterReadingCreationResult.Invalid(validationErrors);
        }

        var status = MeterReadingStatusCalculator.Calculate(reading, batteryLowThreshold);

        database.Readings.Add(reading);

        try
        {
            await database.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is SqliteException sqliteException &&
            sqliteException.SqliteErrorCode == 19)
        {
            logger.LogInformation(
                "Duplicate meter reading rejected for tenant {TenantId} and external ID {ExternalId}",
                reading.TenantId,
                reading.ExternalId);

            return new MeterReadingCreationResult.Duplicate();
        }

        logger.LogInformation(
            "Stored meter reading for tenant {TenantId}, device {DeviceId}, type {Type}",
            reading.TenantId,
            reading.DeviceId,
            reading.Type);

        return new MeterReadingCreationResult.Created(reading, status);
    }
}

public abstract record MeterReadingCreationResult
{
    public sealed record Invalid(IReadOnlyList<string> Errors) : MeterReadingCreationResult;

    public sealed record Duplicate : MeterReadingCreationResult;

    public sealed record Created(
        MeterReading Reading,
        MeterReadingStatus Status) : MeterReadingCreationResult;
}