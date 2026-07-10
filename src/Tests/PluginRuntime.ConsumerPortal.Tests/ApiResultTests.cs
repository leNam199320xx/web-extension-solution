using FluentAssertions;
using FsCheck.Xunit;
using PluginRuntime.ConsumerPortal.Models;
using PluginRuntime.ConsumerPortal.Models.DTOs;

namespace PluginRuntime.ConsumerPortal.Tests;

public class ApiResultTests
{
    [Fact]
    public void Success_IsSuccess()
    {
        var result = ApiResult<string>.Success("hello");
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
        result.Error.Should().BeNull();
    }

    [Fact]
    public void Fail_IsNotSuccess()
    {
        var result = ApiResult<string>.Fail(new ApiError("ERR", "msg", null));
        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("ERR");
    }

    [Fact]
    public void RateLimited_HasRetryAfter()
    {
        var result = ApiResult<string>.RateLimited(60);
        result.IsSuccess.Should().BeFalse();
        result.IsRateLimited.Should().BeTrue();
        result.RetryAfterSeconds.Should().Be(60);
    }

    [Fact]
    public void NetworkFailure_IsNetworkError()
    {
        var result = ApiResult<string>.NetworkFailure();
        result.IsSuccess.Should().BeFalse();
        result.IsNetworkError.Should().BeTrue();
    }

    [Property(MaxTest = 100)]
    public bool Property_Success_AlwaysHasValue(int value)
    {
        var result = ApiResult<int>.Success(value);
        return result.IsSuccess && result.Value == value;
    }

    [Property(MaxTest = 100)]
    public bool Property_RateLimited_RetryAfterIsPositive(FsCheck.PositiveInt seconds)
    {
        var result = ApiResult<string>.RateLimited(seconds.Get);
        return result.RetryAfterSeconds == seconds.Get && result.IsRateLimited;
    }
}
