namespace ApiGateway.Application.RateLimiting;

public readonly record struct RateLimitResult(bool IsAllowed, int RetryAfterSeconds);

public interface IRateLimitStore
{
    Task<RateLimitResult> TryAcquireAsync(string partitionKey, int permitLimit, int windowSeconds, CancellationToken cancellationToken = default);
}
