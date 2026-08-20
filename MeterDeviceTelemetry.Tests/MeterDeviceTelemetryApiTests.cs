using System.Net;
using System.Net.Http.Json;
using MeterDeviceTelemetry.Contracts;
using MeterDeviceTelemetry.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MeterDeviceTelemetry.Tests;

public sealed class MeterDeviceTelemetryApiTests : IDisposable
{
    private readonly string databasePath = Path.Combine(
        Path.GetTempPath(),
        $"meter-device-telemetry-{Guid.NewGuid():N}.db");
    private readonly WebApplicationFactory<Program> factory;
    private readonly HttpClient client;

    public MeterDeviceTelemetryApiTests()
    {
        factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<DbContextOptions<TelemetryDbContext>>();
                    services.RemoveAll<TelemetryDbContext>();
                    services.AddDbContext<TelemetryDbContext>(options =>
                        options.UseSqlite($"Data Source={databasePath}"));
                });
            });

        client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });
    }

    [Fact]
    public async Task PostReading_ReturnsCreatedReadingAndStatus()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/readings")
        {
            Content = JsonContent.Create(new
            {
                tenantId = "acme",
                deviceId = "dev-123",
                type = "water_level",
                value = 1.23,
                unit = "m",
                battery = 62,
                signal = -85,
                recordedAt = "2025-01-10T10:15:00Z",
                externalId = "r-789"
            })
        };
        request.Headers.Add("X-Correlation-ID", "test-correlation-id");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(
            "test-correlation-id",
            response.Headers.GetValues("X-Correlation-ID").Single());

        var body = await response.Content.ReadFromJsonAsync<MeterReadingWithStatusResponse>();

        Assert.NotNull(body);
        Assert.Equal("acme", body!.Reading.TenantId);
        Assert.Equal("dev-123", body.Reading.DeviceId);
        Assert.False(body.Status.BatteryLow);
        Assert.Equal(20, body.Status.BatteryThreshold);
    }

    [Fact]
    public async Task PostReading_WhenRequiredFieldIsMissing_ReturnsBadRequest()
    {
        var response = await client.PostAsJsonAsync("/api/readings", new
        {
            tenantId = "acme",
            deviceId = "dev-123",
            type = "water_level",
            value = 1.23,
            unit = "m",
            signal = -85,
            recordedAt = "2025-01-10T10:15:00Z",
            externalId = "missing-battery"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetReadings_FiltersByTenantAndDeviceAndReturnsNewestFirst()
    {
        var firstReading = new
        {
            tenantId = "acme",
            deviceId = "dev-123",
            type = "water_level",
            value = 1.23,
            unit = "m",
            battery = 62,
            signal = -85,
            recordedAt = "2025-01-10T10:15:00Z",
            externalId = "query-001"
        };

        var secondReading = new
        {
            tenantId = "acme",
            deviceId = "dev-123",
            type = "water_level",
            value = 1.25,
            unit = "m",
            battery = 19,
            signal = -84,
            recordedAt = "2025-01-10T10:20:00Z",
            externalId = "query-002"
        };

        var otherDeviceReading = new
        {
            tenantId = "acme",
            deviceId = "dev-999",
            type = "water_level",
            value = 1.50,
            unit = "m",
            battery = 70,
            signal = -80,
            recordedAt = "2025-01-10T10:25:00Z",
            externalId = "query-003"
        };

        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/readings", firstReading)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/readings", secondReading)).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsJsonAsync("/api/readings", otherDeviceReading)).StatusCode);

        var response = await client.GetAsync("/api/readings?tenantId=acme&deviceId=dev-123");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<PagedMeterReadingsResponse>();

        Assert.NotNull(body);
        Assert.Equal(2, body!.TotalCount);
        Assert.Equal(2, body.Items.Count);
        Assert.Equal("query-002", body.Items[0].Reading.ExternalId);
        Assert.True(body.Items[0].Status.BatteryLow);
        Assert.Equal("query-001", body.Items[1].Reading.ExternalId);
        Assert.False(body.Items[1].Status.BatteryLow);
    }

    public void Dispose()
    {
        client.Dispose();
        factory.Dispose();

        DeleteDatabaseFile(databasePath);
        DeleteDatabaseFile($"{databasePath}-shm");
        DeleteDatabaseFile($"{databasePath}-wal");
    }

    private static void DeleteDatabaseFile(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

}