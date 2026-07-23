using System.Security.Claims;
using ConsultationService.Domain.Enums;

namespace ConsultationService.API.Infrastructure;

public static class UserClaims
{
    public static Guid GetUserId(ClaimsPrincipal user)
    {
        var sub = user.FindFirst("sub")?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(sub, out var id) ? id : throw new UnauthorizedAccessException("Invalid user identity.");
    }

    /// <summary>
    /// PublicId (sub) plus any profile_id claims — consultations may store either.
    /// </summary>
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

    public static ParticipantRole GetParticipantRole(ClaimsPrincipal user)
    {
        var roles = user.FindAll("role")
            .Concat(user.FindAll(ClaimTypes.Role))
            .Select(c => c.Value)
            .ToList();

        if (roles.Any(r => r.Equals("Doctor", StringComparison.OrdinalIgnoreCase)))
            return ParticipantRole.Doctor;

        return ParticipantRole.Patient;
    }

    public static bool IsInAppRole(ClaimsPrincipal user, string role) =>
        user.IsInRole(role) ||
        user.FindAll("role").Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase)) ||
        user.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals(role, StringComparison.OrdinalIgnoreCase));
}
