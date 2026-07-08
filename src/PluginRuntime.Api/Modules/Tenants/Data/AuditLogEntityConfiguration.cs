using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Tenants.Domain;

namespace PluginRuntime.Api.Modules.Tenants.Data;

/// <summary>
/// EF Core configuration for the AuditLogEntry entity.
/// Maps to the "audit_log" table with proper column mappings and indexes.
/// </summary>
public sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntry>
{
    public void Configure(EntityTypeBuilder<AuditLogEntry> builder)
    {
        builder.ToTable("audit_log");

        builder.HasKey(e => e.EntryId);

        builder.Property(e => e.EntryId)
            .HasColumnName("entry_id");

        builder.Property(e => e.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(e => e.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.ActionType)
            .HasColumnName("action_type")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.TargetEntity)
            .HasColumnName("target_entity")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.PreviousState)
            .HasColumnName("previous_state")
            .HasColumnType("text");

        builder.Property(e => e.NewState)
            .HasColumnName("new_state")
            .HasColumnType("text");

        builder.Property(e => e.Reason)
            .HasColumnName("reason")
            .HasColumnType("text");

        builder.Property(e => e.Timestamp)
            .HasColumnName("timestamp");

        // Index on (tenant_id, timestamp DESC) for tenant audit history queries
        builder.HasIndex(e => new { e.TenantId, e.Timestamp })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_log_tenant_id_timestamp");

        // Index on (actor_id, timestamp DESC) for actor audit trail queries
        builder.HasIndex(e => new { e.ActorId, e.Timestamp })
            .IsDescending(false, true)
            .HasDatabaseName("ix_audit_log_actor_id_timestamp");
    }
}
