using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Billing.Domain;

namespace PluginRuntime.Api.Modules.Billing.Data;

/// <summary>
/// EF Core configuration for the UsageAggregate entity.
/// Maps to the "usage_aggregates" table with proper column mappings, indexes, and constraints.
/// </summary>
public sealed class UsageAggregateEntityConfiguration : IEntityTypeConfiguration<UsageAggregate>
{
    public void Configure(EntityTypeBuilder<UsageAggregate> builder)
    {
        builder.ToTable("usage_aggregates");

        builder.HasKey(u => u.AggregateId);

        builder.Property(u => u.AggregateId)
            .HasColumnName("aggregate_id");

        builder.Property(u => u.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(u => u.Date)
            .HasColumnName("date")
            .IsRequired();

        builder.Property(u => u.TotalRequests)
            .HasColumnName("total_requests")
            .IsRequired();

        builder.Property(u => u.SuccessfulRequests)
            .HasColumnName("successful_requests")
            .IsRequired();

        builder.Property(u => u.FailedRequests)
            .HasColumnName("failed_requests")
            .IsRequired();

        builder.Property(u => u.AvgDurationMs)
            .HasColumnName("avg_duration_ms")
            .IsRequired();

        builder.Property(u => u.AggregatedAt)
            .HasColumnName("aggregated_at")
            .IsRequired();

        // Unique constraint on (tenant_id, date)
        builder.HasIndex(u => new { u.TenantId, u.Date })
            .IsUnique()
            .HasDatabaseName("ix_usage_aggregates_tenant_id_date_unique");

        // Index for efficient queries by tenant and date descending
        builder.HasIndex(u => new { u.TenantId, u.Date })
            .IsDescending(false, true)
            .HasDatabaseName("ix_usage_aggregates_tenant_id_date_desc");
    }
}
