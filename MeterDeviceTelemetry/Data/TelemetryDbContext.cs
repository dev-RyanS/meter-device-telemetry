using MeterDeviceTelemetry.Domain;
using Microsoft.EntityFrameworkCore;

namespace MeterDeviceTelemetry.Data;

public sealed class TelemetryDbContext : DbContext
{
    public TelemetryDbContext(DbContextOptions<TelemetryDbContext> options)
        : base(options)
    {
    }

    public DbSet<MeterReading> Readings => Set<MeterReading>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var reading = modelBuilder.Entity<MeterReading>();

        reading.HasKey(item => new { item.TenantId, item.ExternalId });

        reading.Property(item => item.TenantId)
            .IsRequired()
            .HasMaxLength(100);
        reading.Property(item => item.DeviceId)
            .IsRequired()
            .HasMaxLength(100);
        reading.Property(item => item.Type)
            .IsRequired()
            .HasMaxLength(100);
        reading.Property(item => item.Unit)
            .IsRequired()
            .HasMaxLength(50);
        reading.Property(item => item.ExternalId)
            .IsRequired()
            .HasMaxLength(100);

        reading.Property(item => item.RecordedAt)
            .HasConversion(
                value => value.UtcDateTime,
                value => new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc)));
    }
}