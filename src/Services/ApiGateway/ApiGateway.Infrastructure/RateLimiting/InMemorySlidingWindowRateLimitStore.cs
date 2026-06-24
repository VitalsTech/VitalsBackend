using System.Collections.Concurrent;
using ApiGateway.Application.RateLimiting;

namespace ApiGateway.Infrastructure.RateLimiting;

public sealed class InMemorySlidingWindowRateLimitStore : IRateLimitStore
{
    private readonly ConcurrentDictionary<string, List<long>> _windows = new();

    public Task<RateLimitResult> TryAcquireAsync(string key, int permitLimit, int windowSeconds, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = windowSeconds * 1000L;
        var windowStart = now - windowMs;

        var timestamps = _windows.GetOrAdd(key, _ => []);
        lock (timestamps)
        {
            timestamps.RemoveAll(ts => ts <= windowStart);

            if (timestamps.Count >= permitLimit)
            {
                var retryAfter = (int)Math.Ceiling((timestamps[0] + windowMs - now) / 1000.0);
                return Task.FromResult(new RateLimitResult(false, Math.Max(1, retryAfter)));
            }

            timestamps.Add(now);
        }

        return Task.FromResult(new RateLimitResult(true, 0));
    }
}
