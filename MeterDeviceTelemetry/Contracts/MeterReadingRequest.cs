namespace MeterDeviceTelemetry.Contracts;

public sealed class MeterReadingRequest
{
    public string? TenantId { get; init; }
    public string? DeviceId { get; init; }
    public string? Type { get; init; }
    public double? Value { get; init; }
    public string? Unit { get; init; }
    public int? Battery { get; init; }
    public int? Signal { get; init; }
    public DateTimeOffset? RecordedAt { get; init; }
    public string? ExternalId { get; init; }
}