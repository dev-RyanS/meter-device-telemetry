# Meter Device Telemetry Capture

## Config

To set the battery threshold, configure the `Telemetry:BatteryLowThreshold` value in `appsettings.json`.

## Run the API

From the repository root:

```bash
dotnet run --project MeterDeviceTelemetry/MeterDeviceTelemetry.csproj --launch-profile https
```

Once the API is running:
- The API is exposed at `https://localhost:7263`.
- Swagger is available at `https://localhost:7263/swagger`.
- API health check is available at `https://localhost:7263/health`.

## Run the UI

In a second terminal:

```bash
cd MeterDeviceTelemetry.Web
npm install
npm run dev -- --host localhost
```

- The UI is typically available at `http://localhost:5173` or `http://localhost:4173` (see the terminal output for the exact URL).


## Test

```bash
dotnet test MeterDeviceTelemetry.Tests/MeterDeviceTelemetry.Tests.csproj
```

## Azure Pipelines

The pipeline configuration `azure-pipelines.yml` is set up to run on pushes to `main`. But I haven't been able to test this, since it seems to require a paid subscription in Azure DevOps. I then thought to try GitHub Actions, but that also seems to require a paid subscription.


## Correlation ID

To test the correlation ID functionality, you can run the `PostReading_ReturnsCreatedReadingAndStatus` test in `MeterDeviceTelemetry.Tests/MeterDeviceTelemetryApiTests.cs`.
You can also run the API, and then in a separate terminal, run the following to send a request with a custom correlation ID:

```bash
curl -k -i -X POST https://localhost:7263/api/readings \
  -H 'Content-Type: application/json' \
  -H 'X-Correlation-ID: demo-request-001' \
  -d '{
    "tenantId": "acme",
    "deviceId": "dev-123",
    "type": "water_level",
    "value": 1.23,
    "unit": "m",
    "battery": 62,
    "signal": -85,
    "recordedAt": "2025-01-10T10:15:00Z",
    "externalId": "demo-correlation-001"
  }'
```

`-k` allows for localhost self-signed certificates.
`-i` includes the response headers in the output. So, the command will return the response headers, including `X-Correlation-ID`, which should match the value above (`demo-request-001`). This correlation ID should also now appear in the API's console logs.
