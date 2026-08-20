namespace MeterDeviceTelemetry.Configuration;

public sealed class TelemetryOptions
{
    public const string SectionName = "Telemetry";

    public int BatteryLowThreshold { get; init; } = 20;
}