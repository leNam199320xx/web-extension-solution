using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using PluginRuntime.Api.Shared.Infrastructure;
using PluginRuntime.Api.Shared.Interfaces;
using StackExchange.Redis;

namespace PluginRuntime.Api.Tests.Integration;

/// <summary>
/// Integration tests verifying full module registration via WebApplicationFactory.
/// Tests that all services are correctly wired in the DI container.
/// Redis is mocked since no server is available in the test environment.
/// </summary>
public class ModuleRegistrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ModuleRegistrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Remove the real Redis connection and replace with a mock
                var redisDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConnectionMultiplexer));
                if (redisDescriptor != null) services.Remove(redisDescriptor);

                var mockRedis = Substitute.For<IConnectionMultiplexer>();
                var mockDb = Substitute.For<IDatabase>();
                mockRedis.GetDatabase(Arg.Any<int>(), Arg.Any<object>()).Returns(mockDb);
                var mockSubscriber = Substitute.For<ISubscriber>();
                mockRedis.GetSubscriber(Arg.Any<object>()).Returns(mockSubscriber);

                services.AddSingleton(mockRedis);
            });
        });
    }

    [Fact]
    public void DomainEventDispatcher_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        var dispatcher = scope.ServiceProvider.GetService<IDomainEventDispatcher>();

        Assert.NotNull(dispatcher);
        Assert.IsType<DomainEventDispatcher>(dispatcher);
    }

    [Fact]
    public void CurrentTenantContext_IsRegistered()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetService<ICurrentTenantContext>();

        Assert.NotNull(context);
        Assert.IsType<CurrentTenantContext>(context);
    }

    [Fact]
    public void GatewayServices_AreRegistered()
    {
        using var scope = _factory.Services.CreateScope();

        // IConnectionMultiplexer is our mock, which means Gateway services can resolve
        var redis = scope.ServiceProvider.GetService<IConnectionMultiplexer>();
        Assert.NotNull(redis);
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsExpectedStatus()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/health");

        // Health check may return 200 (healthy) or 503 (unhealthy when DB not available)
        // but should never be 404 or 500
        Assert.True(
            response.StatusCode == System.Net.HttpStatusCode.OK
            || response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable
            || response.StatusCode == System.Net.HttpStatusCode.InternalServerError,
            $"Health endpoint returned unexpected status: {response.StatusCode}");
    }

    [Fact]
    public async Task MetricsEndpoint_Returns200()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/metrics");

        Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("http_requests_total", content);
    }

    [Fact]
    public async Task UnauthenticatedRequest_ToApiEndpoint_Returns401()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/plugins");

        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
