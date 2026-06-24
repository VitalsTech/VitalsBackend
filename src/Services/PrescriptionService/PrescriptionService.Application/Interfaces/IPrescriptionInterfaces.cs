using PrescriptionService.Application.DTOs;
using PrescriptionService.Domain.Entities;

namespace PrescriptionService.Application.Interfaces;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Prescription?> GetByIdForUpdateAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task SaveAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task AddStatusHistoryAsync(PrescriptionStatusHistory history, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetExpiringSoonAsync(DateTime thresholdDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Prescription>> GetExpiredCandidatesAsync(DateTime utcNow, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordContextClient
{
    Task<PatientMedicalContextDto?> GetContextAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordEventClient
{
    Task AppendEventAsync(Guid patientId, string eventType, object payload, Guid correlationId, CancellationToken cancellationToken = default);
}

public interface IUserPermissionClient
{
    Task<bool> CanPrescribeAsync(Guid doctorId, string atcCode, CancellationToken cancellationToken = default);
}

public interface IPharmacyIntegrationClient
{
    Task<PharmacyDispatchResultDto> SendPrescriptionAsync(Guid prescriptionId, Guid pharmacyId, Prescription prescription, CancellationToken cancellationToken = default);
}

public interface IEgiszClient
{
    Task<EgiszPreferentialCheckResult> CheckPreferentialEligibilityAsync(
        Guid patientId,
        string? preferentialCategory,
        IReadOnlyList<string> atcCodes,
        CancellationToken cancellationToken = default);
}

public interface IESignatureService
{
    Task<string> SignPrescriptionAsync(Prescription prescription, CancellationToken cancellationToken = default);
}

public interface IPrescriptionValidationEngine
{
    PrescriptionValidationResultDto Validate(CreatePrescriptionRequest request, PatientMedicalContextDto? context, bool doctorConfirmedWarnings);
}

public interface IPrescriptionQrService
{
    QrCodeResponse Generate(Guid prescriptionId, string signature);
}

public interface IPatientInstructionGenerator
{
    IReadOnlyList<PatientInstructionDto> Generate(IReadOnlyList<MedicationItemDto> medications);
}

public interface IPrescriptionEventPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}

public interface IPrescriptionService
{
    Task<PrescriptionResponse> CreateDraftAsync(Guid doctorId, CreatePrescriptionRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionValidationResultDto> ValidateAsync(Guid prescriptionId, bool confirmWarnings, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> SignAsync(Guid prescriptionId, Guid doctorId, bool confirmWarnings, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> SendToPharmacyAsync(Guid prescriptionId, Guid userId, SendToPharmacyRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> CancelAsync(Guid prescriptionId, Guid userId, string reason, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> RecordFulfillmentAsync(Guid prescriptionId, FulfillmentRequest request, CancellationToken cancellationToken = default);
    Task<PrescriptionResponse> GetAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PrescriptionResponse>> GetPatientPrescriptionsAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<QrCodeResponse> GetQrCodeAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PatientInstructionDto>> GetPatientInstructionsAsync(Guid prescriptionId, CancellationToken cancellationToken = default);
}
