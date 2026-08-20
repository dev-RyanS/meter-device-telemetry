# Meter Device Telemetry Capture

## Config

To set the battery threshold, configure the `Telemetry:BatteryLowThreshold` value in `appsettings.json`.

## Run the API

From the repository root:

```bash
dotnet run --project MeterDeviceTelemetry/MeterDeviceTelemetry.csproj --launch-profile https
```

## Run the UI

In a second terminal:

```bash
cd MeterDeviceTelemetry.Web
npm install
npm run dev -- --host localhost
```

## Test

```bash
dotnet test MeterDeviceTelemetry.Tests/MeterDeviceTelemetry.Tests.csproj
```
