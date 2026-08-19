namespace MeterDeviceTelemetry.Domain;

public sealed record MeterReading(
    string TenantId,
    string DeviceId,
    string Type,
    double Value,
    string Unit,
    int Battery,
    int Signal,
    DateTimeOffset RecordedAt,
    string ExternalId);