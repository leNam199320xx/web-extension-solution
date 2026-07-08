using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Modules.Tenants.Data;

/// <summary>
/// EF Core configuration for the Tenant entity.
/// Maps to the "tenants" table with proper column mappings, indexes, and constraints.
/// </summary>
public sealed class TenantEntityConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");

        builder.HasKey(t => t.TenantId);

        builder.Property(t => t.TenantId)
            .HasColumnName("tenant_id");

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.ContactEmail)
            .HasConversion(
                e => e.Value,
                v => new Email(v))
            .HasColumnName("contact_email")
            .HasMaxLength(254)
            .IsRequired();

        builder.Property(t => t.CompanyName)
            .HasColumnName("company_name")
            .HasMaxLength(200);

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.PlanId)
            .HasColumnName("plan_id");

        builder.Property(t => t.PendingPlanId)
            .HasColumnName("pending_plan_id");

        builder.Property(t => t.StripeCustomerId)
            .HasColumnName("stripe_customer_id")
            .HasMaxLength(200);

        builder.Property(t => t.StripeSubscriptionId)
            .HasColumnName("stripe_subscription_id")
            .HasMaxLength(200);

        builder.Property(t => t.IsInternal)
            .HasColumnName("is_internal")
            .HasDefaultValue(false);

        builder.Property(t => t.Version)
            .HasColumnName("version")
            .HasDefaultValue(0L);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at");

        // Foreign keys to plans table
        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(t => t.PendingPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on contact_email for active tenants
        builder.HasIndex(t => t.ContactEmail)
            .IsUnique()
            .HasDatabaseName("ix_tenants_contact_email_unique");
    }
}
