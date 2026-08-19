namespace MeterDeviceTelemetry.Domain;

public static class MeterReadingValidator
{
    public static IReadOnlyList<string> Validate(MeterReading reading)
    {
        var errors = new List<string>();

        AddRequiredError(errors, reading.TenantId, nameof(reading.TenantId));
        AddRequiredError(errors, reading.DeviceId, nameof(reading.DeviceId));
        AddRequiredError(errors, reading.Type, nameof(reading.Type));
        AddRequiredError(errors, reading.Unit, nameof(reading.Unit));
        AddRequiredError(errors, reading.ExternalId, nameof(reading.ExternalId));

        if (!double.IsFinite(reading.Value))
        {
            errors.Add("Value must be a finite number.");
        }

        if (reading.Battery is < 0 or > 100)
        {
            errors.Add("Battery must be between 0 and 100.");
        }

        if (reading.Signal is < -150 or > 0)
        {
            errors.Add("Signal must be between -150 and 0 dBm.");
        }

        if (reading.RecordedAt.Offset != TimeSpan.Zero)
        {
            errors.Add("RecordedAt must be expressed in UTC.");
        }

        return errors;
    }

    private static void AddRequiredError(List<string> errors, string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add($"{fieldName} is required.");
        }
    }
}