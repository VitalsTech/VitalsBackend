using System.Security.Claims;

namespace UserService.API.Infrastructure;

public static class UserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Неверный идентификатор пользователя.");
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

    public static bool IsDoctor(ClaimsPrincipal user) =>
        user.IsInRole("Doctor") ||
        user.FindAll("role").Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase)) ||
        user.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase));
}
