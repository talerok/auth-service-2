using Auth.Api;
using Auth.Api.RateLimit;
using Auth.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using StackExchange.Redis;
using System.Net;

namespace Auth.UnitTests.RateLimit;

public sealed class RateLimitMiddlewareTests
{
    private readonly Mock<IConnectionMultiplexer> _redis = new();
    private readonly Mock<IDatabase> _db = new();
    private readonly RateLimitOptions _options = new();

    public RateLimitMiddlewareTests()
    {
        _redis.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_db.Object);
    }

    private RateLimitMiddleware CreateMiddleware(RequestDelegate next) =>
        new(next, _redis.Object, Options.Create(_options), NullLogger<RateLimitMiddleware>.Instance);

    private static DefaultHttpContext CreateHttpContext(string path = "/connect/login", string ip = "192.168.1.1")
    {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        context.Connection.RemoteIpAddress = IPAddress.Parse(ip);
        return context;
    }

    [Fact]
    public async Task Invoke_UnderLimit_CallsNext()
    {
        _options.Auth = new RateLimitPolicyOptions { PermitLimit = 5, WindowSeconds = 60 };

        _db.Setup(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create((RedisValue)1));

        var nextCalled = false;
        var endpoint = CreateEndpointWithRateLimit("auth");
        var context = CreateHttpContext();
        context.SetEndpoint(endpoint);

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeTrue();
        context.Response.StatusCode.Should().NotBe(429);
    }

    [Fact]
    public async Task Invoke_OverLimit_Returns429WithRetryAfter()
    {
        _options.Auth = new RateLimitPolicyOptions { PermitLimit = 5, WindowSeconds = 60 };

        _db.Setup(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create((RedisValue)6));

        var nextCalled = false;
        var endpoint = CreateEndpointWithRateLimit("auth");
        var context = CreateHttpContext();
        context.SetEndpoint(endpoint);
        context.Response.Body = new MemoryStream();

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeFalse();
        context.Response.StatusCode.Should().Be(429);
        context.Response.Headers.RetryAfter.ToString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Invoke_PolicyDisabled_CallsNext()
    {
        _options.Auth = new RateLimitPolicyOptions { PermitLimit = 0, WindowSeconds = 60 };

        var nextCalled = false;
        var endpoint = CreateEndpointWithRateLimit("auth");
        var context = CreateHttpContext();
        context.SetEndpoint(endpoint);

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeTrue();
        _db.Verify(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_NoAttribute_FallsBackToGlobal()
    {
        _options.Global = new RateLimitPolicyOptions { PermitLimit = 100, WindowSeconds = 60 };

        _db.Setup(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(RedisResult.Create((RedisValue)1));

        var nextCalled = false;
        var endpoint = new Endpoint(_ => Task.CompletedTask, new EndpointMetadataCollection(), "test");
        var context = CreateHttpContext("/api/users");
        context.SetEndpoint(endpoint);

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task Invoke_GlobalDisabled_NoAttributeSkipsCheck()
    {
        _options.Global = new RateLimitPolicyOptions { PermitLimit = 0, WindowSeconds = 60 };

        var nextCalled = false;
        var context = CreateHttpContext("/api/users");

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeTrue();
        _db.Verify(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()), Times.Never);
    }

    [Fact]
    public async Task Invoke_RedisFailure_AllowsRequest()
    {
        _options.Auth = new RateLimitPolicyOptions { PermitLimit = 5, WindowSeconds = 60 };

        _db.Setup(d => d.ScriptEvaluateAsync(It.IsAny<LuaScript>(), It.IsAny<object>(), It.IsAny<CommandFlags>()))
            .ThrowsAsync(new RedisConnectionException(ConnectionFailureType.UnableToConnect, "test"));

        var nextCalled = false;
        var endpoint = CreateEndpointWithRateLimit("auth");
        var context = CreateHttpContext();
        context.SetEndpoint(endpoint);

        var middleware = CreateMiddleware(_ => { nextCalled = true; return Task.CompletedTask; });
        await middleware.Invoke(context);

        nextCalled.Should().BeTrue();
    }

    private static Endpoint CreateEndpointWithRateLimit(string policy)
    {
        var metadata = new EndpointMetadataCollection(new RateLimitAttribute(policy));
        return new Endpoint(_ => Task.CompletedTask, metadata, "test");
    }
}
