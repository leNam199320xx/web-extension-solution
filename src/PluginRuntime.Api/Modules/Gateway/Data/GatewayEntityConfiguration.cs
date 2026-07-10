using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Gateway.Domain;

namespace PluginRuntime.Api.Modules.Gateway.Data;

/// <summary>
/// EF Core configuration for Gateway module entities:
/// plugin_access and failed_notifications tables.
/// </summary>
public sealed class PluginAccessConfiguration : IEntityTypeConfiguration<PluginAccess>
{
    public void Configure(EntityTypeBuilder<PluginAccess> builder)
    {
        builder.ToTable("plugin_access");

        builder.HasKey(pa => new { pa.TenantId, pa.PluginId });

        builder.Property(pa => pa.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(pa => pa.PluginId)
            .HasColumnName("plugin_id")
            .IsRequired();

        builder.Property(pa => pa.Source)
            .HasColumnName("source")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(pa => pa.PackageId)
            .HasColumnName("package_id");

        builder.Property(pa => pa.GrantedAt)
            .HasColumnName("granted_at")
            .IsRequired();

        builder.HasIndex(pa => pa.TenantId)
            .HasDatabaseName("idx_plugin_access_tenant");

        builder.HasIndex(pa => pa.PluginId)
            .HasDatabaseName("idx_plugin_access_plugin");

        builder.HasIndex(pa => pa.PackageId)
            .HasDatabaseName("idx_plugin_access_package");
    }
}

public sealed class FailedNotificationConfiguration : IEntityTypeConfiguration<FailedNotification>
{
    public void Configure(EntityTypeBuilder<FailedNotification> builder)
    {
        builder.ToTable("failed_notifications");

        builder.HasKey(fn => fn.NotificationId);

        builder.Property(fn => fn.NotificationId)
            .HasColumnName("notification_id")
            .ValueGeneratedNever();

        builder.Property(fn => fn.Channel)
            .HasColumnName("channel")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(fn => fn.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(fn => fn.RetryCount)
            .HasColumnName("retry_count")
            .IsRequired();

        builder.Property(fn => fn.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        builder.Property(fn => fn.ProcessedAt)
            .HasColumnName("processed_at");

        builder.HasIndex(fn => fn.ProcessedAt)
            .HasDatabaseName("idx_failed_notifications_unprocessed")
            .HasFilter("processed_at IS NULL");
    }
}
