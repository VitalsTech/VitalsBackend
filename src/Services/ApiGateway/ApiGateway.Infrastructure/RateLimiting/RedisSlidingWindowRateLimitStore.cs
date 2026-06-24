using StackExchange.Redis;
using ApiGateway.Application.RateLimiting;

namespace ApiGateway.Infrastructure.RateLimiting;

public sealed class RedisSlidingWindowRateLimitStore : IRateLimitStore
{
    private const string AcquireScript = @"
local key = KEYS[1]
local window_ms = tonumber(ARGV[1])
local limit = tonumber(ARGV[2])
local now = tonumber(ARGV[3])
local member = ARGV[4]
redis.call('ZREMRANGEBYSCORE', key, 0, now - window_ms)
local count = redis.call('ZCARD', key)
if count < limit then
  redis.call('ZADD', key, now, member)
  redis.call('PEXPIRE', key, window_ms)
  return {1, 0}
end
local oldest = redis.call('ZRANGE', key, 0, 0, 'WITHSCORES')
local retry_after = 1
if oldest[2] then
  retry_after = math.max(1, math.ceil((tonumber(oldest[2]) + window_ms - now) / 1000))
end
return {0, retry_after}";

    private readonly IConnectionMultiplexer _redis;

    public RedisSlidingWindowRateLimitStore(IConnectionMultiplexer redis) => _redis = redis;

    public async Task<RateLimitResult> TryAcquireAsync(
        string partitionKey,
        int permitLimit,
        int windowSeconds,
        CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = windowSeconds * 1000L;
        var member = $"{nowMs}:{Guid.NewGuid():N}";

        var raw = await db.ScriptEvaluateAsync(
            AcquireScript,
            [$"ratelimit:{partitionKey}"],
            [windowMs, permitLimit, nowMs, member]);

        if (raw.IsNull)
            return new RateLimitResult(true, 0);

        var result = (RedisResult[])raw!;
        return new RateLimitResult((int)result[0] == 1, (int)result[1]);
    }
}
