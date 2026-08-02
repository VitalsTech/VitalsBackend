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

    private static readonly HashSet<string> DoctorClinicalScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        AccessScopes.ReadHistory,
        AccessScopes.ReadProjections,
        AccessScopes.WriteEvents
    };

    /// <summary>MVP: любой врач с ролью Doctor может читать state/history без grant.</summary>
    private static readonly HashSet<string> DoctorOpenReadScopes = new(StringComparer.OrdinalIgnoreCase)
    {
        AccessScopes.ReadHistory,
        AccessScopes.ReadProjections
    };

    private readonly IAccessGrantRepository _grants;
    private readonly IDoctorRecipientResolver _doctorAccess;

    public AccessControlService(IAccessGrantRepository grants, IDoctorRecipientResolver doctorAccess)
    {
        _grants = grants;
        _doctorAccess = doctorAccess;
    }

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

        var granteeIds = new List<Guid> { actor.UserId };
        granteeIds.AddRange(actor.ProfileIds.Where(id => id != Guid.Empty && id != actor.UserId));

        foreach (var granteeId in granteeIds)
        {
            var grants = await _grants.GetActiveGrantsAsync(patientId, granteeId, cancellationToken);
            foreach (var grant in grants)
            {
                var scopes = JsonSerializer.Deserialize<List<string>>(grant.ScopesJson) ?? new List<string>();
                if (scopes.Contains(requiredScope, StringComparer.OrdinalIgnoreCase))
                    return;
            }
        }

        if (actor.Roles.Contains("Doctor", StringComparer.OrdinalIgnoreCase))
        {
            // Чтение медкарты (state/history/attachments) — открыто для роли Doctor (MVP телемедицины).
            if (DoctorOpenReadScopes.Contains(requiredScope))
                return;

            // Запись событий — только grant или консультация с пациентом.
            if (DoctorClinicalScopes.Contains(requiredScope))
            {
                var allowed = await _doctorAccess
                    .DoctorHasAccessAsync(patientId, granteeIds, cancellationToken)
                    .ConfigureAwait(false);
                if (allowed)
                    return;
            }
        }

        throw new AccessDeniedException();
    }
}
