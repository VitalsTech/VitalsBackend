using ApiGateway.Application.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace ApiGateway.Infrastructure.RateLimiting;

public sealed class RateLimitPolicyResolver
{
    private readonly RateLimitingOptions _options;

    public RateLimitPolicyResolver(IOptions<RateLimitingOptions> options) => _options = options.Value;

    public string? ResolvePolicyName(HttpContext context)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        foreach (var rule in _options.EndpointRules)
        {
            if (!PathMatches(path, rule.Path))
                continue;
            if (rule.Methods.Length > 0 && !rule.Methods.Contains(method, StringComparer.OrdinalIgnoreCase))
                continue;
            return rule.Policy;
        }

        return null;
    }

    private static bool PathMatches(string requestPath, string rulePath)
    {
        if (string.Equals(requestPath, rulePath, StringComparison.OrdinalIgnoreCase))
            return true;
        return requestPath.StartsWith(rulePath.TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
    }

    public RateLimitPolicyOptions GetDefaultPolicy(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true &&
            _options.Policies.TryGetValue("Authenticated", out var authenticated))
            return authenticated;

        return _options.Policies.TryGetValue("Anonymous", out var anonymous)
            ? anonymous
            : new RateLimitPolicyOptions { PermitLimit = 100, WindowSeconds = 60 };
    }

    public RateLimitPolicyOptions GetPolicy(string policyName) =>
        _options.Policies.TryGetValue(policyName, out var policy)
            ? policy
            : throw new InvalidOperationException($"Rate limit policy '{policyName}' is not configured.");
}
