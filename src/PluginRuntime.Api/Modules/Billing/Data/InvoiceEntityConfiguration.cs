using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Modules.Billing.Domain;

namespace PluginRuntime.Api.Modules.Billing.Data;

/// <summary>
/// EF Core configuration for the Invoice entity.
/// Maps to the "invoices" table with proper column mappings, indexes, and constraints.
/// </summary>
public sealed class InvoiceEntityConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("invoices");

        builder.HasKey(i => i.InvoiceId);

        builder.Property(i => i.InvoiceId)
            .HasColumnName("invoice_id");

        builder.Property(i => i.TenantId)
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(i => i.BillingPeriodStart)
            .HasColumnName("billing_period_start")
            .IsRequired();

        builder.Property(i => i.BillingPeriodEnd)
            .HasColumnName("billing_period_end")
            .IsRequired();

        builder.Property(i => i.BaseAmount)
            .HasColumnName("base_amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.OverageAmount)
            .HasColumnName("overage_amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.PackageAmount)
            .HasColumnName("package_amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.TotalAmount)
            .HasColumnName("total_amount")
            .HasPrecision(10, 2)
            .IsRequired();

        builder.Property(i => i.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(i => i.StripeInvoiceId)
            .HasColumnName("stripe_invoice_id")
            .HasMaxLength(200);

        builder.Property(i => i.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        // Foreign key to tenants table
        builder.HasIndex(i => new { i.TenantId, i.BillingPeriodStart })
            .IsDescending(false, true)
            .HasDatabaseName("ix_invoices_tenant_id_billing_period_start_desc");
    }
}
