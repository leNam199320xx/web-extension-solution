using PluginRuntime.ConsumerPortal.Models.DTOs;

namespace PluginRuntime.ConsumerPortal.Models;

/// <summary>
/// Result pattern for all API calls. Supports Success, ApiError, RateLimited, and NetworkError states.
/// </summary>
public sealed record ApiResult<T>
{
    public T? Value { get; init; }
    public ApiError? Error { get; init; }
    public int? RetryAfterSeconds { get; init; }
    public bool IsSuccess => Error is null && !IsRateLimited && !IsNetworkError;
    public bool IsRateLimited { get; init; }
    public bool IsNetworkError { get; init; }

    public static ApiResult<T> Success(T value) => new() { Value = value };
    public static ApiResult<T> Fail(ApiError error) => new() { Error = error };
    public static ApiResult<T> RateLimited(int retryAfter) => new() { IsRateLimited = true, RetryAfterSeconds = retryAfter };
    public static ApiResult<T> NetworkFailure() => new() { IsNetworkError = true };
}
