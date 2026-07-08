using Microsoft.EntityFrameworkCore;
using PluginRuntime.Api.Modules.Billing.Domain;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Tenants.Domain;
using PluginRuntime.Api.Shared.Entities;

namespace PluginRuntime.Api.Shared.Infrastructure;

/// <summary>
/// Application database context providing access to shared platform entities.
/// Applies entity configurations from the assembly automatically via OnModelCreating.
/// </summary>
public sealed class AppDbContext : DbContext
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<PluginPackage> PluginPackages => Set<PluginPackage>();
    public DbSet<PackagePlugin> PackagePlugins => Set<PackagePlugin>();
    public DbSet<PackageSubscription> PackageSubscriptions => Set<PackageSubscription>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<UsageAggregate> UsageAggregates => Set<UsageAggregate>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
