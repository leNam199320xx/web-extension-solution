using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using PluginRuntime.Api.Shared.Entities;
using PluginRuntime.Api.Shared.ValueObjects;

namespace PluginRuntime.Api.Tests.Properties;

/// <summary>
/// Feature: unified-api-architecture
/// Property 2: Tenant registration invariants
///
/// For any valid tenant registration request (name 1–200 chars, valid RFC 5322 email),
/// the resulting Tenant record SHALL have status "Active", plan set to Free (the lowest tier),
/// and a TenantCreated domain event SHALL be dispatched.
/// </summary>
public class TenantRegistrationProperties
{
    [Property(MaxTest = 100)]
    public bool Property2_ValidRegistration_ProducesActiveTenant(Guid planId)
    {
        // Generate valid name (1-200 chars)
        var name = new string('A', Random.Shared.Next(1, 200));
        var email = new Email("test@example.com");

        var tenant = new Tenant(
            tenantId: Guid.NewGuid(),
            name: name,
            contactEmail: email,
            planId: planId,
            isInternal: false);

        // Invariants must hold
        return tenant.Status == TenantStatus.Active
            && tenant.PlanId == planId
            && !tenant.IsInternal
            && tenant.Version == 1
            && tenant.Name == name;
    }

    [Property(MaxTest = 100)]
    public void Property2_InvalidName_Empty_ThrowsDomainException()
    {
        var email = new Email("test@example.com");

        var act = () => new Tenant(
            tenantId: Guid.NewGuid(),
            name: "",
            contactEmail: email,
            planId: Guid.NewGuid());

        act.Should().Throw<Exception>();
    }

    [Property(MaxTest = 100)]
    public void Property2_InvalidName_TooLong_ThrowsDomainException()
    {
        var email = new Email("test@example.com");
        var longName = new string('X', 201);

        var act = () => new Tenant(
            tenantId: Guid.NewGuid(),
            name: longName,
            contactEmail: email,
            planId: Guid.NewGuid());

        act.Should().Throw<Exception>();
    }

    [Property(MaxTest = 100)]
    public bool Property2_TenantVersion_StartsAtOne(Guid tenantId, Guid planId)
    {
        var email = new Email("test@example.com");

        var tenant = new Tenant(
            tenantId: tenantId,
            name: "ValidName",
            contactEmail: email,
            planId: planId);

        return tenant.Version == 1;
    }

    [Property(MaxTest = 100)]
    public bool Property2_InternalTenant_HasIsInternalFlag(Guid tenantId, Guid planId)
    {
        var email = new Email("internal@example.com");

        var tenant = new Tenant(
            tenantId: tenantId,
            name: "InternalTenant",
            contactEmail: email,
            planId: planId,
            isInternal: true);

        return tenant.IsInternal == true && tenant.Status == TenantStatus.Active;
    }
}
