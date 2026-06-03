using System.Text.Json;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Domain.Entities;
using MedicalRecordService.Domain.Enums;
using MedicalRecordService.Domain.Interfaces;

namespace MedicalRecordService.Application.Services;

public sealed class AccessGrantService : IAccessGrantService
{
    private readonly IAccessGrantRepository _grants;
    private readonly IAccessControlService _accessControl;
    private readonly IAuditLogRepository _audit;

    public AccessGrantService(
        IAccessGrantRepository grants,
        IAccessControlService accessControl,
        IAuditLogRepository audit)
    {
        _grants = grants;
        _accessControl = accessControl;
        _audit = audit;
    }

    public async Task<AccessGrantDto> CreateGrantAsync(
        Guid patientId,
        CreateAccessGrantRequest request,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _accessControl.EnsureAccessAsync(patientId, actor, AccessScopes.ManageConsents, cancellationToken);

        var grant = new AccessGrant
        {
            PatientId = patientId,
            GranteeId = request.GranteeId,
            GranteeType = request.GranteeType,
            ScopesJson = JsonSerializer.Serialize(request.Scopes),
            RestrictedCategoriesJson = request.RestrictedCategories is null
                ? null
                : JsonSerializer.Serialize(request.RestrictedCategories),
            ExpiresAt = request.ExpiresAt,
            GrantedByUserId = actor.UserId
        };

        await _grants.AddAsync(grant, cancellationToken);
        await _grants.SaveChangesAsync(cancellationToken);

        await _audit.AddAsync(new AuditLogEntry
        {
            PatientId = patientId,
            ActorUserId = actor.UserId,
            ActorType = "User",
            ActionType = AuditActionTypes.AccessGrantCreated,
            DetailsJson = JsonSerializer.Serialize(new { grant.Id, request.GranteeId }),
            IpAddress = actor.IpAddress,
            SessionId = actor.SessionId
        }, cancellationToken);
        await _audit.SaveChangesAsync(cancellationToken);

        return Map(grant);
    }

    public async Task RevokeGrantAsync(
        Guid patientId,
        Guid grantId,
        ActorContext actor,
        CancellationToken cancellationToken = default)
    {
        await _accessControl.EnsureAccessAsync(patientId, actor, AccessScopes.ManageConsents, cancellationToken);

        var grant = await _grants.GetByIdAsync(grantId, cancellationToken);
        if (grant is null || grant.PatientId != patientId)
            return;

        grant.RevokedAt = DateTime.UtcNow;
        await _grants.SaveChangesAsync(cancellationToken);

        await _audit.AddAsync(new AuditLogEntry
        {
            PatientId = patientId,
            ActorUserId = actor.UserId,
            ActorType = "User",
            ActionType = AuditActionTypes.AccessGrantRevoked,
            DetailsJson = JsonSerializer.Serialize(new { grantId }),
            IpAddress = actor.IpAddress,
            SessionId = actor.SessionId
        }, cancellationToken);
        await _audit.SaveChangesAsync(cancellationToken);
    }

    private static AccessGrantDto Map(AccessGrant g) => new()
    {
        Id = g.Id,
        GranteeId = g.GranteeId,
        GranteeType = g.GranteeType,
        Scopes = JsonSerializer.Deserialize<List<string>>(g.ScopesJson) ?? new List<string>(),
        ExpiresAt = g.ExpiresAt,
        CreatedAt = g.CreatedAt
    };
}
