using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Tenants.Data;

/// <summary>
/// EF Core configuration for the ApiKey entity.
/// Maps to the "api_keys" table with proper column mappings, indexes, and constraints.
/// </summary>
public sealed class ApiKeyEntityConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> builder)
    {
        builder.ToTable("api_keys");

        builder.HasKey(k => k.KeyId);

        builder.Property(k => k.KeyId)
            .HasColumnName("key_id");

        builder.Property(k => k.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(k => k.KeyHash)
            .HasColumnName("key_hash")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(k => k.KeyPrefix)
            .HasColumnName("key_prefix")
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(k => k.KeySuffix)
            .HasColumnName("key_suffix")
            .HasMaxLength(4)
            .IsRequired();

        builder.Property(k => k.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(k => k.ExpiresAt)
            .HasColumnName("expires_at");

        builder.Property(k => k.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(k => k.RevokedAt)
            .HasColumnName("revoked_at");

        // Foreign key to tenants table
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(k => k.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index on key_hash for fast lookup
        builder.HasIndex(k => k.KeyHash)
            .IsUnique()
            .HasDatabaseName("ix_api_keys_key_hash_unique");

        // Index on tenant_id for listing keys by tenant
        builder.HasIndex(k => k.TenantId)
            .HasDatabaseName("ix_api_keys_tenant_id");
    }
}
