using MeterDeviceTelemetry.Domain;

namespace MeterDeviceTelemetry.Contracts;

public sealed record MeterReadingResponse(
    string TenantId,
    string DeviceId,
    string Type,
    double Value,
    string Unit,
    int Battery,
    int Signal,
    DateTimeOffset RecordedAt,
    string ExternalId);

public sealed record MeterReadingStatusResponse(
    bool BatteryLow,
    int BatteryThreshold);

public sealed record MeterReadingWithStatusResponse(
    MeterReadingResponse Reading,
    MeterReadingStatusResponse Status);

public sealed record PagedMeterReadingsResponse(
    IReadOnlyList<MeterReadingWithStatusResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public static class MeterReadingResponseMapper
{
    public static MeterReadingWithStatusResponse Map(
        MeterReading reading,
        MeterReadingStatus status)
    {
        var readingResponse = new MeterReadingResponse(
            reading.TenantId,
            reading.DeviceId,
            reading.Type,
            reading.Value,
            reading.Unit,
            reading.Battery,
            reading.Signal,
            reading.RecordedAt,
            reading.ExternalId);

        var statusResponse = new MeterReadingStatusResponse(
            status.BatteryLow,
            status.BatteryThreshold);

        return new MeterReadingWithStatusResponse(readingResponse, statusResponse);
    }
}