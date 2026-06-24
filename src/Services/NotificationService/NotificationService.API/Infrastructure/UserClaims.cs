using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.API.Infrastructure;

public static class UserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Invalid user identity.");
    }
}
