using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace MedicalRecordService.Infrastructure.Services;

public sealed class DoctorRecipientResolver : IDoctorRecipientResolver
{
    private readonly IAccessGrantRepository _grants;
    private readonly IConsultationDoctorClient _consultation;
    private readonly IUserIdentityClient _identities;
    private readonly ILogger<DoctorRecipientResolver> _logger;

    public DoctorRecipientResolver(
        IAccessGrantRepository grants,
        IConsultationDoctorClient consultation,
        IUserIdentityClient identities,
        ILogger<DoctorRecipientResolver> logger)
    {
        _grants = grants;
        _consultation = consultation;
        _identities = identities;
        _logger = logger;
    }

    public async Task<bool> DoctorHasAccessAsync(
        Guid patientId,
        IReadOnlyList<Guid> doctorIdentityIds,
        CancellationToken cancellationToken = default)
    {
        if (doctorIdentityIds.Count == 0)
            return false;

        var patientIds = await _identities.ResolveIdentityIdsAsync(patientId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var pid in patientIds)
        {
            var grants = await _grants.GetActiveDoctorGrantsForPatientAsync(pid, cancellationToken)
                .ConfigureAwait(false);
            if (grants.Any(g => doctorIdentityIds.Contains(g.GranteeId)))
                return true;
        }

        if (await _consultation
                .DoctorHasSessionWithPatientAsync(doctorIdentityIds, patientIds, cancellationToken)
                .ConfigureAwait(false))
            return true;

        // Fallback: latest consultation doctor for any patient alias
        foreach (var pid in patientIds)
        {
            var latestDoctor = await _consultation.GetLatestDoctorIdAsync(pid, cancellationToken)
                .ConfigureAwait(false);
            if (latestDoctor is { } doctorId && doctorIdentityIds.Contains(doctorId))
                return true;
        }

        return false;
    }

    public async Task<IReadOnlyList<Guid>> ResolveDoctorIdsAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var ids = await ResolveDoctorIdsCoreAsync(patientId, cancellationToken).ConfigureAwait(false);

        if (ids.Count == 0)
        {
            _logger.LogInformation(
                "mood_notify_no_recipients patientId={PatientId}",
                patientId);
        }
        else
        {
            _logger.LogInformation(
                "Resolved {Count} doctor recipients for patient {PatientId}: {DoctorIds}",
                ids.Count,
                patientId,
                string.Join(',', ids));
        }

        return ids;
    }

    private async Task<List<Guid>> ResolveDoctorIdsCoreAsync(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var ids = new HashSet<Guid>();
        var patientIds = await _identities.ResolveIdentityIdsAsync(patientId, cancellationToken)
            .ConfigureAwait(false);

        foreach (var pid in patientIds)
        {
            var grants = await _grants.GetActiveDoctorGrantsForPatientAsync(pid, cancellationToken)
                .ConfigureAwait(false);
            foreach (var grant in grants)
                ids.Add(grant.GranteeId);

            var latestDoctor = await _consultation.GetLatestDoctorIdAsync(pid, cancellationToken)
                .ConfigureAwait(false);
            if (latestDoctor.HasValue)
                ids.Add(latestDoctor.Value);
        }

        return ids.ToList();
    }
}
