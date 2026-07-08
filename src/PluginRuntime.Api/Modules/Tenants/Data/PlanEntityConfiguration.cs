using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Modules.Tenants.Data;

/// <summary>
/// EF Core configuration for the Plan entity.
/// Maps to the "plans" table with seed data for Free, Pro, Enterprise, and Internal plans.
/// </summary>
public sealed class PlanEntityConfiguration : IEntityTypeConfiguration<Plan>
{
    // Well-known plan IDs for seed data
    private static readonly Guid FreePlanId = new("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ProPlanId = new("00000000-0000-0000-0000-000000000002");
    private static readonly Guid EnterprisePlanId = new("00000000-0000-0000-0000-000000000003");
    private static readonly Guid InternalPlanId = new("00000000-0000-0000-0000-000000000004");

    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");

        builder.HasKey(p => p.PlanId);

        builder.Property(p => p.PlanId)
            .HasColumnName("plan_id");

        builder.Property(p => p.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Type)
            .HasColumnName("type")
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.RateLimit)
            .HasColumnName("rate_limit");

        builder.Property(p => p.DailyQuota)
            .HasColumnName("daily_quota");

        builder.Property(p => p.MaxApiKeys)
            .HasColumnName("max_api_keys");

        builder.Property(p => p.MaxPluginsUpload)
            .HasColumnName("max_plugins_upload");

        builder.Property(p => p.MaxPackageSubscriptions)
            .HasColumnName("max_package_subscriptions");

        builder.Property(p => p.MonthlyPrice)
            .HasColumnName("monthly_price")
            .HasPrecision(10, 2);

        builder.Property(p => p.OverageRatePer1k)
            .HasColumnName("overage_rate_per_1k")
            .HasPrecision(10, 4);

        builder.Property(p => p.StripePriceId)
            .HasColumnName("stripe_price_id")
            .HasMaxLength(200);

        builder.Property(p => p.IsBillable)
            .HasColumnName("is_billable")
            .HasDefaultValue(true);

        builder.Property(p => p.FeaturesJson)
            .HasColumnName("features_json")
            .HasColumnType("text");

        builder.Property(p => p.CreatedAt)
            .HasColumnName("created_at");

        // Unique index on plan name
        builder.HasIndex(p => p.Name)
            .IsUnique()
            .HasDatabaseName("ix_plans_name_unique");

        // Seed data
        builder.HasData(
            CreatePlan(FreePlanId, "Free", PlanType.Free,
                rateLimit: 100, dailyQuota: 1000, maxApiKeys: 2,
                maxPluginsUpload: 5, maxPackageSubscriptions: 0,
                monthlyPrice: 0m, overageRatePer1k: null,
                stripePriceId: null, isBillable: false),
            CreatePlan(ProPlanId, "Pro", PlanType.Pro,
                rateLimit: 1000, dailyQuota: 50000, maxApiKeys: 10,
                maxPluginsUpload: 50, maxPackageSubscriptions: 10,
                monthlyPrice: 49.99m, overageRatePer1k: 0.50m,
                stripePriceId: "price_pro_monthly", isBillable: true),
            CreatePlan(EnterprisePlanId, "Enterprise", PlanType.Enterprise,
                rateLimit: null, dailyQuota: null, maxApiKeys: null,
                maxPluginsUpload: null, maxPackageSubscriptions: null,
                monthlyPrice: 499.99m, overageRatePer1k: null,
                stripePriceId: "price_enterprise_monthly", isBillable: true),
            CreatePlan(InternalPlanId, "Internal", PlanType.Internal,
                rateLimit: null, dailyQuota: null, maxApiKeys: null,
                maxPluginsUpload: null, maxPackageSubscriptions: null,
                monthlyPrice: 0m, overageRatePer1k: null,
                stripePriceId: null, isBillable: false)
        );
    }

    private static object CreatePlan(
        Guid planId, string name, PlanType type,
        int? rateLimit, int? dailyQuota, int? maxApiKeys,
        int? maxPluginsUpload, int? maxPackageSubscriptions,
        decimal monthlyPrice, decimal? overageRatePer1k,
        string? stripePriceId, bool isBillable)
    {
        // Using anonymous type for HasData — EF Core requires this for entities with private setters
        return new
        {
            PlanId = planId,
            Name = name,
            Type = type,
            RateLimit = rateLimit,
            DailyQuota = dailyQuota,
            MaxApiKeys = maxApiKeys,
            MaxPluginsUpload = maxPluginsUpload,
            MaxPackageSubscriptions = maxPackageSubscriptions,
            MonthlyPrice = monthlyPrice,
            OverageRatePer1k = overageRatePer1k,
            StripePriceId = stripePriceId,
            IsBillable = isBillable,
            FeaturesJson = (string?)null,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
    }
}
