using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Subscriptions.Domain;

namespace PluginRuntime.Api.Modules.Subscriptions.Data;

/// <summary>
/// EF Core configuration for the PluginPackage entity.
/// </summary>
public sealed class PluginPackageConfiguration : IEntityTypeConfiguration<PluginPackage>
{
    public void Configure(EntityTypeBuilder<PluginPackage> builder)
    {
        builder.ToTable("plugin_packages");

        builder.HasKey(p => p.PackageId);

        builder.Property(p => p.PackageId)
            .HasColumnName("package_id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasColumnName("description")
            .HasMaxLength(2000);

        builder.Property(p => p.MonthlyPrice)
            .HasColumnName("monthly_price")
            .HasPrecision(18, 2);

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(p => p.StripePriceId)
            .HasColumnName("stripe_price_id")
            .HasMaxLength(255);

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(p => p.UpdatedAt)
            .HasColumnName("updated_at");

        builder.HasMany(p => p.Plugins)
            .WithOne()
            .HasForeignKey(pp => pp.PackageId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => p.Status)
            .HasDatabaseName("ix_plugin_packages_status");
    }
}

/// <summary>
/// EF Core configuration for the PackagePlugin join entity.
/// </summary>
public sealed class PackagePluginConfiguration : IEntityTypeConfiguration<PackagePlugin>
{
    public void Configure(EntityTypeBuilder<PackagePlugin> builder)
    {
        builder.ToTable("package_plugins");

        builder.HasKey(pp => new { pp.PackageId, pp.PluginId });

        builder.Property(pp => pp.PackageId)
            .HasColumnName("package_id");

        builder.Property(pp => pp.PluginId)
            .HasColumnName("plugin_id");

        builder.Property(pp => pp.AddedAt)
            .HasColumnName("added_at");
    }
}
