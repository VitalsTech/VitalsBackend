using System.Security.Claims;

namespace AITriageService.API.Infrastructure;

public static class UserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Invalid user identity.");
    }

    public static IReadOnlyList<Guid> GetIdentityIds(ClaimsPrincipal user)
    {
        var ids = new List<Guid> { GetUserId(user) };
        foreach (var claim in user.FindAll("profile_id"))
        {
            if (Guid.TryParse(claim.Value, out var profileId) &&
                profileId != Guid.Empty &&
                !ids.Contains(profileId))
            {
                ids.Add(profileId);
            }
        }

        return ids;
    }

    public static bool IsInAppRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role) ||
        user.FindAll("role").Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)) ||
        user.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
}
