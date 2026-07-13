using FsCheck;
using FsCheck.Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PluginRuntime.Api.Shared.Events;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;

namespace PluginRuntime.Api.Tests.Properties;

/// <summary>
/// Feature: unified-api-architecture
/// Property 1: Domain event dispatch completeness
///
/// For any domain event dispatched via IDomainEventDispatcher, all registered
/// IDomainEventHandler implementations for that event type SHALL receive the event
/// with the exact payload, and no handlers for other event types SHALL be invoked.
/// </summary>
public class EventDispatchProperties
{
    [Property(MaxTest = 100)]
    public async Task Property1_AllRegisteredHandlers_ReceiveExactPayload(Guid tenantId, string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;

        // Arrange: register multiple handlers for TenantCreated
        var handler1 = Substitute.For<IDomainEventHandler<TenantCreated>>();
        var handler2 = Substitute.For<IDomainEventHandler<TenantCreated>>();
        var unrelatedHandler = Substitute.For<IDomainEventHandler<PlanChanged>>();

        var services = new ServiceCollection();
        services.AddScoped<IDomainEventHandler<TenantCreated>>(_ => handler1);
        services.AddScoped<IDomainEventHandler<TenantCreated>>(_ => handler2);
        services.AddScoped<IDomainEventHandler<PlanChanged>>(_ => unrelatedHandler);

        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider, NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TenantCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenantId,
            Name: name,
            ContactEmail: "test@example.com",
            IsInternal: false);

        // Act
        await dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert: both handlers received the exact payload
        await handler1.Received().HandleAsync(
            Arg.Is<TenantCreated>(e => e != null && e.EventId == domainEvent.EventId && e.TenantId == tenantId),
            Arg.Any<CancellationToken>());

        await handler2.Received().HandleAsync(
            Arg.Is<TenantCreated>(e => e != null && e.EventId == domainEvent.EventId && e.TenantId == tenantId),
            Arg.Any<CancellationToken>());

        // Assert: unrelated handler was NOT invoked
        await unrelatedHandler.DidNotReceive().HandleAsync(
            Arg.Any<PlanChanged>(),
            Arg.Any<CancellationToken>());
    }

    [Property(MaxTest = 100)]
    public async Task Property1_FailingHandler_DoesNotPreventOtherHandlers(Guid tenantId)
    {
        // Arrange: one handler throws, the other should still be invoked
        var failingHandler = Substitute.For<IDomainEventHandler<TenantCreated>>();
        failingHandler.HandleAsync(Arg.Any<TenantCreated>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("Simulated failure")));

        var successHandler = Substitute.For<IDomainEventHandler<TenantCreated>>();

        var services = new ServiceCollection();
        services.AddScoped<IDomainEventHandler<TenantCreated>>(_ => failingHandler);
        services.AddScoped<IDomainEventHandler<TenantCreated>>(_ => successHandler);

        var provider = services.BuildServiceProvider();
        var dispatcher = new DomainEventDispatcher(provider, NullLogger<DomainEventDispatcher>.Instance);

        var domainEvent = new TenantCreated(
            EventId: Guid.NewGuid(),
            OccurredAt: DateTime.UtcNow,
            TenantId: tenantId,
            Name: "TestTenant",
            ContactEmail: "test@example.com",
            IsInternal: false);

        // Act — should not throw
        await dispatcher.DispatchAsync(domainEvent, CancellationToken.None);

        // Assert: the success handler was still invoked
        await successHandler.Received(1).HandleAsync(
            Arg.Is<TenantCreated>(e => e != null && e.EventId == domainEvent.EventId),
            Arg.Any<CancellationToken>());
    }
}
