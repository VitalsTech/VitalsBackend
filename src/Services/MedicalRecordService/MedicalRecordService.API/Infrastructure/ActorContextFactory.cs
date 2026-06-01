using System.Security.Claims;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.API.Infrastructure;

public static class ActorContextFactory
{
    public static ActorContext FromHttpContext(HttpContext httpContext, IOptions<JwtValidationOptions> jwtOptions)
    {
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var sub = user.FindFirstValue("sub") ?? user.FindFirstValue(ClaimTypes.NameIdentifier);
            return new ActorContext
            {
                UserId = Guid.TryParse(sub, out var id) ? id : Guid.Empty,
                Roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList(),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                SessionId = httpContext.TraceIdentifier
            };
        }

        if (jwtOptions.Value.AllowDevelopmentHeaderFallback &&
            httpContext.Request.Headers.TryGetValue("X-User-Id", out var devUser) &&
            Guid.TryParse(devUser.FirstOrDefault(), out var devId))
        {
            var roles = httpContext.Request.Headers["X-User-Roles"].FirstOrDefault()?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            return new ActorContext
            {
                UserId = devId,
                Roles = roles,
                IsSystemService = httpContext.Request.Headers.ContainsKey("X-Service-Name"),
                ServiceName = httpContext.Request.Headers["X-Service-Name"].FirstOrDefault(),
                IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
                SessionId = httpContext.TraceIdentifier
            };
        }

        return new ActorContext { UserId = Guid.Empty };
    }
}
