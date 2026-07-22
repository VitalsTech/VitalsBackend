using System.Text.Json;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Exceptions;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Domain.Interfaces;
using MedicalRecordService.Domain.Enums;

namespace MedicalRecordService.Application.Services;

public sealed class AccessControlService : IAccessControlService
{
    private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Service", "Admin"
    };

    private readonly IAccessGrantRepository _grants;

    public AccessControlService(IAccessGrantRepository grants) => _grants = grants;

    public bool IsPatientSelf(Guid patientId, ActorContext actor)
    {
        if (!actor.Roles.Contains("Patient", StringComparer.OrdinalIgnoreCase))
            return false;

        // Medical-record patientId may be either user PublicId (JWT sub) or Patient ProfileId.
        return actor.UserId == patientId
            || actor.ProfileIds.Contains(patientId);
    }

    public async Task EnsureAccessAsync(
        Guid patientId,
        ActorContext actor,
        string requiredScope,
        CancellationToken cancellationToken = default)
    {
        if (actor.IsSystemService || actor.Roles.Any(SystemRoles.Contains))
            return;

        if (IsPatientSelf(patientId, actor))
        {
            if (requiredScope is AccessScopes.ManageConsents or AccessScopes.WriteEvents or AccessScopes.ReadHistory or AccessScopes.ReadProjections)
                return;
        }

        if (requiredScope == AccessScopes.ManageConsents && IsPatientSelf(patientId, actor))
            return;

        var grants = await _grants.GetActiveGrantsAsync(patientId, actor.UserId, cancellationToken);
        foreach (var grant in grants)
        {
            var scopes = JsonSerializer.Deserialize<List<string>>(grant.ScopesJson) ?? new List<string>();
            if (scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
                return;
        }

        throw new AccessDeniedException();
    }
}
