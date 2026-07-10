using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Subscriptions.Data;

/// <summary>
/// EF Core configuration for the PackageSubscription entity.
/// </summary>
public sealed class PackageSubscriptionEntityConfiguration : IEntityTypeConfiguration<PackageSubscription>
{
    public void Configure(EntityTypeBuilder<PackageSubscription> builder)
    {
        builder.ToTable("package_subscriptions");

        builder.HasKey(s => s.SubscriptionId);

        builder.Property(s => s.SubscriptionId)
            .HasColumnName("subscription_id");

        builder.Property(s => s.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(s => s.PackageId)
            .HasColumnName("package_id")
            .IsRequired();

        builder.Property(s => s.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(s => s.StripeSubscriptionItemId)
            .HasColumnName("stripe_subscription_item_id")
            .HasMaxLength(200);

        builder.Property(s => s.StartDate)
            .HasColumnName("start_date");

        builder.Property(s => s.EndDate)
            .HasColumnName("end_date");

        builder.Property(s => s.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(s => s.UpdatedAt)
            .HasColumnName("updated_at");

        // Unique constraint: one subscription per tenant per package
        builder.HasAlternateKey(s => new { s.TenantId, s.PackageId });

        // Foreign key to tenants table
        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to plugin_packages table
        builder.HasOne<PluginPackage>()
            .WithMany()
            .HasForeignKey(s => s.PackageId)
            .OnDelete(DeleteBehavior.Restrict);

        // Index: tenant + status (for listing active subscriptions per tenant)
        builder.HasIndex(s => new { s.TenantId, s.Status })
            .HasDatabaseName("ix_package_subscriptions_tenant_status");

        // Index: package + status (for finding all subscribers to a package)
        builder.HasIndex(s => new { s.PackageId, s.Status })
            .HasDatabaseName("ix_package_subscriptions_package_status");
    }
}
