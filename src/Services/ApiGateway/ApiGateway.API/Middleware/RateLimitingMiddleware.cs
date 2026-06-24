using System.Security.Claims;
using ApiGateway.Application.Options;
using ApiGateway.Application.RateLimiting;
using ApiGateway.Infrastructure.RateLimiting;
using Microsoft.Extensions.Options;

namespace ApiGateway.API.Middleware;

public sealed class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IRateLimitStore _store;
    private readonly RateLimitPolicyResolver _resolver;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(
        RequestDelegate next,
        IRateLimitStore store,
        RateLimitPolicyResolver resolver,
        ILogger<RateLimitingMiddleware> logger)
    {
        _next = next;
        _store = store;
        _resolver = resolver;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var policyName = _resolver.ResolvePolicyName(context);
        var policy = policyName is not null
            ? _resolver.GetPolicy(policyName)
            : _resolver.GetDefaultPolicy(context);

        var partition = BuildPartition(context, policy, policyName ?? "default");
        var key = $"{policyName ?? (context.User.Identity?.IsAuthenticated == true ? "auth" : "anon")}:{partition}";

        var result = await _store.TryAcquireAsync(key, policy.PermitLimit, policy.WindowSeconds, context.RequestAborted);
        if (!result.IsAllowed)
        {
            _logger.LogWarning("Rate limit exceeded for {Partition} [{RequestId}]", partition, context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers.RetryAfter = result.RetryAfterSeconds.ToString();
            await context.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests. Please try again later.",
                retryAfterSeconds = result.RetryAfterSeconds
            });
            return;
        }

        await _next(context);
    }

    private static string BuildPartition(HttpContext context, RateLimitPolicyOptions policy, string label)
    {
        if (policy.PartitionBy == RateLimitPartitionKind.UserId && context.User.Identity?.IsAuthenticated == true)
        {
            var sub = context.User.FindFirst("sub")?.Value ?? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(sub))
                return $"user:{sub}";
        }

        var ip = context.Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return $"ip:{ip}:{label}";
    }
}
