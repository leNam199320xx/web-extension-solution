using FluentAssertions;
using PluginRuntime.Api.Modules.Gateway.Domain;
using PluginRuntime.Api.Modules.Subscriptions.Domain;
using PluginRuntime.Api.Modules.Tenants.Domain;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.Infrastructure.Persistence;
using PluginRuntime.Api.Shared.Interfaces;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Tests.Integration;

/// <summary>
/// Tests the JSON file-based repository to verify it works correctly as a persistence provider.
/// Uses a temp directory for each test to ensure isolation.
/// </summary>
public class JsonProviderTests : IDisposable
{
    private readonly string _tempDir;

    public JsonProviderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"json_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task AddAndGetById_Tenant()
    {
        IRepository<Tenant> repo = new JsonRepository<Tenant>(_tempDir);

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: "Test Tenant",
            contactEmail: new Email("test@example.com"),
            planId: Guid.NewGuid(),
            companyName: "Test Corp");

        await repo.AddAsync(tenant);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(tenant.TenantId);
        loaded.Should().NotBeNull();
        loaded!.Name.Should().Be("Test Tenant");
        loaded.Status.Should().Be(TenantStatus.Active);
    }

    [Fact]
    public async Task FindAsync_FiltersCorrectly()
    {
        IRepository<Plan> repo = new JsonRepository<Plan>(_tempDir);

        // We can't construct Plan directly (private setters), so test with a type we can create
        IRepository<Tenant> tenantRepo = new JsonRepository<Tenant>(_tempDir);

        var t1 = new Tenant(Guid.NewGuid(), "Tenant A", new Email("a@x.com"), Guid.NewGuid(), isInternal: false);
        var t2 = new Tenant(Guid.NewGuid(), "Tenant B", new Email("b@x.com"), Guid.NewGuid(), isInternal: true);
        var t3 = new Tenant(Guid.NewGuid(), "Tenant C", new Email("c@x.com"), Guid.NewGuid(), isInternal: false);

        await tenantRepo.AddRangeAsync([t1, t2, t3]);
        await tenantRepo.SaveChangesAsync();

        var internals = await tenantRepo.FindAsync(t => t.IsInternal);
        internals.Should().HaveCount(1);
        internals[0].Name.Should().Be("Tenant B");
    }

    [Fact]
    public async Task CountAsync_ReturnsCorrectCount()
    {
        IRepository<Tenant> repo = new JsonRepository<Tenant>(_tempDir);

        await repo.AddRangeAsync([
            new Tenant(Guid.NewGuid(), "T1", new Email("t1@x.com"), Guid.NewGuid()),
            new Tenant(Guid.NewGuid(), "T2", new Email("t2@x.com"), Guid.NewGuid()),
            new Tenant(Guid.NewGuid(), "T3", new Email("t3@x.com"), Guid.NewGuid()),
        ]);
        await repo.SaveChangesAsync();

        var total = await repo.CountAsync();
        total.Should().Be(3);

        var filtered = await repo.CountAsync(t => t.Name == "T2");
        filtered.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges()
    {
        IRepository<Tenant> repo = new JsonRepository<Tenant>(_tempDir);

        var tenant = new Tenant(Guid.NewGuid(), "Original", new Email("orig@x.com"), Guid.NewGuid());
        await repo.AddAsync(tenant);
        await repo.SaveChangesAsync();

        tenant.Suspend();
        await repo.UpdateAsync(tenant);
        await repo.SaveChangesAsync();

        // Re-read from fresh repo instance (simulates app restart)
        IRepository<Tenant> repo2 = new JsonRepository<Tenant>(_tempDir);
        var loaded = await repo2.GetByIdAsync(tenant.TenantId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(TenantStatus.Suspended);
    }

    [Fact]
    public async Task RemoveAsync_DeletesEntity()
    {
        IRepository<Tenant> repo = new JsonRepository<Tenant>(_tempDir);

        var tenant = new Tenant(Guid.NewGuid(), "ToDelete", new Email("del@x.com"), Guid.NewGuid());
        await repo.AddAsync(tenant);
        await repo.SaveChangesAsync();

        await repo.RemoveAsync(tenant);
        await repo.SaveChangesAsync();

        var all = await repo.GetAllAsync();
        all.Should().BeEmpty();
    }

    [Fact]
    public async Task Query_ReturnsQueryable()
    {
        IRepository<Tenant> repo = new JsonRepository<Tenant>(_tempDir);

        await repo.AddRangeAsync([
            new Tenant(Guid.NewGuid(), "Alpha", new Email("alpha@x.com"), Guid.NewGuid()),
            new Tenant(Guid.NewGuid(), "Beta", new Email("beta@x.com"), Guid.NewGuid()),
            new Tenant(Guid.NewGuid(), "Gamma", new Email("gamma@x.com"), Guid.NewGuid()),
        ]);
        await repo.SaveChangesAsync();

        var result = repo.Query()
            .Where(t => t.Name.StartsWith("A") || t.Name.StartsWith("G"))
            .OrderBy(t => t.Name)
            .ToList();

        result.Should().HaveCount(2);
        result[0].Name.Should().Be("Alpha");
        result[1].Name.Should().Be("Gamma");
    }

    [Fact]
    public async Task UnitOfWork_ProvidesRepositories()
    {
        IUnitOfWork uow = new JsonUnitOfWork(_tempDir);

        var tenantRepo = uow.Repository<Tenant>();
        var tenant = new Tenant(Guid.NewGuid(), "UoW Tenant", new Email("uow@x.com"), Guid.NewGuid());
        await tenantRepo.AddAsync(tenant);
        await uow.SaveChangesAsync();

        var loaded = await tenantRepo.GetByIdAsync(tenant.TenantId);
        loaded.Should().NotBeNull();
    }

    [Fact]
    public async Task PluginAccess_CrudOperations()
    {
        IRepository<PluginAccess> repo = new JsonRepository<PluginAccess>(_tempDir);

        var tenantId = Guid.NewGuid();
        var pluginId1 = Guid.NewGuid();
        var pluginId2 = Guid.NewGuid();

        await repo.AddRangeAsync([
            PluginAccess.Create(tenantId, pluginId1, AccessSource.Free),
            PluginAccess.Create(tenantId, pluginId2, AccessSource.Package, Guid.NewGuid()),
        ]);
        await repo.SaveChangesAsync();

        var entries = await repo.FindAsync(pa => pa.TenantId == tenantId);
        entries.Should().HaveCount(2);

        // Remove all
        await repo.RemoveRangeAsync(entries);
        await repo.SaveChangesAsync();

        var remaining = await repo.GetAllAsync();
        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task PackageSubscription_CrudOperations()
    {
        IRepository<PackageSubscription> repo = new JsonRepository<PackageSubscription>(_tempDir);

        var sub = PackageSubscription.Create(Guid.NewGuid(), Guid.NewGuid(), "stripe_item_123");
        await repo.AddAsync(sub);
        await repo.SaveChangesAsync();

        var loaded = await repo.GetByIdAsync(sub.SubscriptionId);
        loaded.Should().NotBeNull();
        loaded!.Status.Should().Be(SubscriptionStatus.Active);

        sub.Cancel();
        await repo.UpdateAsync(sub);
        await repo.SaveChangesAsync();

        var updated = await repo.GetByIdAsync(sub.SubscriptionId);
        updated!.Status.Should().Be(SubscriptionStatus.Cancelled);
    }
}
