using System.Text.Json.Serialization;

namespace MeterDeviceTelemetry.Contracts;

public sealed class MeterReadingRequest
{
    [JsonRequired]
    public required string TenantId { get; init; }

    [JsonRequired]
    public required string DeviceId { get; init; }

    [JsonRequired]
    public required string Type { get; init; }

    [JsonRequired]
    public required double Value { get; init; }

    [JsonRequired]
    public required string Unit { get; init; }

    [JsonRequired]
    public required int Battery { get; init; }

    [JsonRequired]
    public required int Signal { get; init; }

    [JsonRequired]
    public required DateTimeOffset RecordedAt { get; init; }

    [JsonRequired]
    public required string ExternalId { get; init; }
}