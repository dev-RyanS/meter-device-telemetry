using System.Net;
using System.Net.Http.Json;
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

        var body = await response.Content.ReadFromJsonAsync<CreateReadingResponse>();

        Assert.NotNull(body);
        Assert.Equal("acme", body!.Reading.TenantId);
        Assert.Equal("dev-123", body.Reading.DeviceId);
        Assert.False(body.Status.BatteryLow);
        Assert.Equal(20, body.Status.BatteryThreshold);
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

    private sealed record CreateReadingResponse(
        ReadingResponse Reading,
        StatusResponse Status);

    private sealed record ReadingResponse(
        string TenantId,
        string DeviceId);

    private sealed record StatusResponse(
        bool BatteryLow,
        int BatteryThreshold);
}