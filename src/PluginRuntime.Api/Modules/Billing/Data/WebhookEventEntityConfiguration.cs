using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Billing.Domain;

namespace PluginRuntime.Api.Modules.Billing.Data;

/// <summary>
/// EF Core configuration for the WebhookEvent entity.
/// Maps to the "webhook_events" table with proper column mappings, indexes, and constraints.
/// </summary>
public sealed class WebhookEventEntityConfiguration : IEntityTypeConfiguration<WebhookEvent>
{
    public void Configure(EntityTypeBuilder<WebhookEvent> builder)
    {
        builder.ToTable("webhook_events");

        builder.HasKey(w => w.EventId);

        builder.Property(w => w.EventId)
            .HasColumnName("event_id");

        builder.Property(w => w.StripeEventId)
            .HasColumnName("stripe_event_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.Payload)
            .HasColumnName("payload")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(w => w.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(w => w.ReceivedAt)
            .HasColumnName("received_at")
            .IsRequired();

        builder.Property(w => w.ProcessedAt)
            .HasColumnName("processed_at");

        // Unique index on stripe_event_id for idempotent processing
        builder.HasIndex(w => w.StripeEventId)
            .IsUnique()
            .HasDatabaseName("ix_webhook_events_stripe_event_id_unique");
    }
}
