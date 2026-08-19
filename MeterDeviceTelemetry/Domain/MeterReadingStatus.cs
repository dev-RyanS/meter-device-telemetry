namespace MeterDeviceTelemetry.Domain;

public sealed record MeterReadingStatus(bool BatteryLow, int BatteryThreshold);

public static class MeterReadingStatusCalculator
{
    public static MeterReadingStatus Calculate(MeterReading reading, int batteryThreshold)
    {
        if (batteryThreshold is < 0 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(batteryThreshold));
        }

        var batteryIsLow = reading.Battery < batteryThreshold;

        return new MeterReadingStatus(batteryIsLow, batteryThreshold);
    }
}