using MeterDeviceTelemetry.Domain;

namespace MeterDeviceTelemetry.Tests;

public class MeterReadingDomainTests
{
    [Fact]
    public void ValidReading_HasNoValidationErrors()
    {
        var reading = new MeterReading(
            "acme",
            "dev-123",
            "water_level",
            1.23,
            "m",
            62,
            -85,
            new DateTimeOffset(2025, 1, 10, 10, 15, 0, TimeSpan.Zero),
            "r-789");

        var errors = MeterReadingValidator.Validate(reading);

        Assert.Empty(errors);
    }

    [Fact]
    public void InvalidReading_ReturnsValidationErrors()
    {
        var reading = new MeterReading(
            "",
            "",
            "",
            double.NaN,
            "",
            -1,
            1,
            new DateTimeOffset(2025, 1, 10, 10, 15, 0, TimeSpan.FromHours(1)),
            "");

        var errors = MeterReadingValidator.Validate(reading);

        Assert.Equal(9, errors.Count);
        Assert.Contains("TenantId is required.", errors);
        Assert.Contains("DeviceId is required.", errors);
        Assert.Contains("Type is required.", errors);
        Assert.Contains("Unit is required.", errors);
        Assert.Contains("ExternalId is required.", errors);
        Assert.Contains("Value must be a finite number.", errors);
        Assert.Contains("Battery must be between 0 and 100.", errors);
        Assert.Contains("Signal must be between -150 and 0 dBm.", errors);
        Assert.Contains("RecordedAt must be expressed in UTC.", errors);
    }

    [Fact]
    public void BatteryBelowThreshold_IsLow()
    {
        var reading = new MeterReading(
            "acme",
            "dev-123",
            "water_level",
            1.23,
            "m",
            19,
            -85,
            new DateTimeOffset(2025, 1, 10, 10, 15, 0, TimeSpan.Zero),
            "r-789");

        var status = MeterReadingStatusCalculator.Calculate(reading, 20);

        Assert.True(status.BatteryLow);
        Assert.Equal(20, status.BatteryThreshold);
    }

    [Fact]
    public void BatteryAtThreshold_IsNotLow()
    {
        var reading = new MeterReading(
            "acme",
            "dev-123",
            "water_level",
            1.23,
            "m",
            20,
            -85,
            new DateTimeOffset(2025, 1, 10, 10, 15, 0, TimeSpan.Zero),
            "r-789");

        var status = MeterReadingStatusCalculator.Calculate(reading, 20);

        Assert.False(status.BatteryLow);
    }

    [Fact]
    public void InvalidBatteryThreshold_ThrowsException()
    {
        var reading = new MeterReading(
            "acme",
            "dev-123",
            "water_level",
            1.23,
            "m",
            20,
            -85,
            new DateTimeOffset(2025, 1, 10, 10, 15, 0, TimeSpan.Zero),
            "r-789");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            MeterReadingStatusCalculator.Calculate(reading, 101));
    }
}