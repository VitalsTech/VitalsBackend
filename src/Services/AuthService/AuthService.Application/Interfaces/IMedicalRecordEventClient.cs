using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IMedicalRecordEventClient
{
    Task ImportEsiaSnapshotAsync(Guid patientId, EsiaUserInfo esiaUser, CancellationToken cancellationToken = default);
}
