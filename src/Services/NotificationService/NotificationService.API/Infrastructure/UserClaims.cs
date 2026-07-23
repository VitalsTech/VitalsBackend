using System.Security.Claims;

namespace NotificationService.API.Infrastructure;

public static class UserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Invalid user identity.");
    }

    /// <summary>
    /// PublicId (sub) plus profile_id claims — notifications may be stored under either id.
    /// </summary>
    public static IReadOnlyList<Guid> GetIdentityIds(ClaimsPrincipal user)
    {
        var ids = new HashSet<Guid> { GetUserId(user) };

        foreach (var claim in user.FindAll("profile_id"))
        {
            if (Guid.TryParse(claim.Value, out var profileId))
                ids.Add(profileId);
        }

        return ids.ToList();
    }
}
