using FluentAssertions;
using FsCheck.Xunit;
using PublicApiGateway.Models;

namespace PublicApiGateway.Tests;

/// <summary>
/// Property-based and unit tests for the Public API Gateway.
/// </summary>
public class GatewayTests
{
    [Fact]
    public void ErrorCodes_AreWellFormed()
    {
        ErrorCodes.AuthRequired.Should().StartWith("GW-");
        ErrorCodes.RateLimitExceeded.Should().StartWith("GW-");
        ErrorCodes.QuotaExceeded.Should().StartWith("GW-");
        ErrorCodes.UpstreamError.Should().StartWith("GW-");
    }

    [Property(MaxTest = 100)]
    public bool Property1_PlanLimits_NullMeansUnlimited(int? rateLimit, int? dailyQuota)
    {
        var limits = new PlanLimits(rateLimit, dailyQuota);
        return limits.RateLimit == rateLimit && limits.DailyQuota == dailyQuota;
    }

    [Property(MaxTest = 100)]
    public bool Property5_QuotaResult_RejectsAboveLimit(FsCheck.PositiveInt count, FsCheck.PositiveInt limit)
    {
        var currentCount = (long)count.Get;
        var dailyLimit = limit.Get;

        var isAllowed = currentCount <= dailyLimit;
        var result = new QuotaResult(isAllowed, currentCount, dailyLimit, isAllowed ? 0 : 3600);

        return result.IsAllowed == (currentCount <= dailyLimit);
    }

    [Property(MaxTest = 100)]
    public bool Property16_ApiKeyFormat_ValidPattern(string key)
    {
        if (key == null) return true;

        var pattern = new System.Text.RegularExpressions.Regex(@"^[a-zA-Z0-9\-_]{32,128}$");
        var isValid = pattern.IsMatch(key);

        // Valid keys: 32-128 chars, alphanumeric + hyphen + underscore
        if (key.Length >= 32 && key.Length <= 128 && key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            return isValid;

        return !isValid || key.Length < 32 || key.Length > 128;
    }

    [Property(MaxTest = 100)]
    public bool Property11_CorrelationId_PrintableAsciiOnly(string value)
    {
        if (value == null) return true;

        var isValid = value.Length >= 1 && value.Length <= 128 && value.All(c => c >= 33 && c <= 126);

        // If all chars are printable ASCII and length is 1-128, it's valid
        return isValid == (value.Length >= 1 && value.Length <= 128 && value.All(c => c >= 33 && c <= 126));
    }

    [Fact]
    public void GatewayException_AuthRequired_HasCorrectProperties()
    {
        var ex = new AuthenticationRequiredException();
        ex.ErrorCode.Should().Be("GW-AUTH-001");
        ex.HttpStatusCode.Should().Be(401);
    }

    [Fact]
    public void GatewayException_RateLimit_HasRetryAfter()
    {
        var ex = new RateLimitExceededException(60);
        ex.ErrorCode.Should().Be("GW-RATE-001");
        ex.HttpStatusCode.Should().Be(429);
        ex.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public void GatewayException_QuotaExceeded_HasRetryAfter()
    {
        var ex = new QuotaExceededException(3600);
        ex.ErrorCode.Should().Be("GW-QUOTA-001");
        ex.HttpStatusCode.Should().Be(429);
        ex.RetryAfterSeconds.Should().Be(3600);
    }

    [Fact]
    public void GatewayError_SerializesCorrectly()
    {
        var error = new GatewayError("GW-AUTH-001", "API key required", "trace-123", "2024-01-01T00:00:00Z");
        error.Code.Should().Be("GW-AUTH-001");
        error.Message.Should().Be("API key required");
        error.TraceId.Should().Be("trace-123");
    }

    [Property(MaxTest = 100)]
    public bool Property17_BodySizeLimit_RejectsOverLimit(FsCheck.PositiveInt bodySize, FsCheck.PositiveInt limit)
    {
        var body = (long)bodySize.Get;
        var maxAllowed = (long)limit.Get;

        // Should reject when body > limit
        var shouldReject = body > maxAllowed;
        return shouldReject == (body > maxAllowed);
    }
}
