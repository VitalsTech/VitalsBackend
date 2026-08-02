using RoutingService.Application.DTOs;
using RoutingService.Domain.Entities;

namespace RoutingService.Application.Interfaces;

public interface IRoutingEngine
{
    RoutingEngineResult Evaluate(RoutingEngineInput input);
}

public interface IDoctorScheduler
{
    Task<DoctorSlotDto?> FindAvailableDoctorAsync(string specialty, int urgencyLevel, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordContextClient
{
    Task<PatientMedicalContextDto?> GetContextAsync(Guid patientId, CancellationToken cancellationToken = default);
}

public interface IRoutingDecisionRepository
{
    Task<RoutingDecision?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoutingDecision?> GetLatestByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task SaveDecisionWithAuditAsync(RoutingDecision decision, RoutingAuditEntry audit, CancellationToken cancellationToken = default);
    Task UpdateAsync(RoutingDecision decision, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ClinicRoutingRule>> GetActiveRulesAsync(string clinicId, CancellationToken cancellationToken = default);
}

public interface IPatientRouteRepository
{
    Task<PatientRoute?> GetActiveByPatientIdAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task SaveAsync(PatientRoute route, CancellationToken cancellationToken = default);
}

public interface IRoutingEventPublisher
{
    Task PublishAsync(string topic, object payload, CancellationToken cancellationToken = default);
}

public interface IConsultationDispatchClient
{
    Task<Guid?> CreateFromRoutingDecisionAsync(
        Guid decisionId,
        TriageCompletedEventDto triageEvent,
        RoutingEngineResult result,
        DoctorSlotDto doctor,
        CancellationToken cancellationToken = default);
}

public interface ILabOrderDispatchClient
{
    Task CreateFromRoutingAsync(
        Guid patientId,
        Guid? doctorId,
        Guid? consultationId,
        IReadOnlyList<string> labs,
        string priority,
        CancellationToken cancellationToken = default);
}

public interface IRoutingOrchestrator
{
    Task<RoutingDecisionResponse> ProcessTriageCompletedAsync(TriageCompletedEventDto triageEvent, CancellationToken cancellationToken = default);
    Task<PatientActiveRouteResponse?> GetActiveRouteAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task<PatientActiveRouteResponse> AppendPostConsultationLabsAsync(
        Guid patientId,
        AppendPostConsultationLabsRequest request,
        CancellationToken cancellationToken = default);
}
