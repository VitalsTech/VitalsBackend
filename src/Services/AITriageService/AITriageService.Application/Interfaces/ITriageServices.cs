using AITriageService.Application.DTOs;

namespace AITriageService.Application.Interfaces;

public interface ISymptomParser
{
    IReadOnlyList<ExtractedEntityDto> Parse(string text);
}

public interface INerService
{
    Task<IReadOnlyList<NerEntityDto>> ExtractAsync(string text, IReadOnlyList<ExtractedEntityDto> parsed, CancellationToken cancellationToken = default);
}

public interface ILlmTriageService
{
    Task<LlmTriageResultDto> AnalyzeAsync(TriagePromptContext context, CancellationToken cancellationToken = default);
}

public interface IMedicalRecordContextClient
{
    Task<PatientMedicalContextDto?> GetContextAsync(Guid patientId, CancellationToken cancellationToken = default);
    Task AppendTriageCompletedEventAsync(Guid patientId, Guid sessionId, LlmTriageResultDto result, CancellationToken cancellationToken = default);
    Task<bool> DoctorHasAccessAsync(Guid patientId, IReadOnlyList<Guid> doctorIdentityIds, CancellationToken cancellationToken = default);
}

public interface ITriageOrchestrator
{
    Task<TriageSessionResponse> CreateSessionAsync(CreateTriageSessionRequest request, CancellationToken cancellationToken = default);
    Task<TriageSessionResponse> ProcessMessageAsync(Guid sessionId, SendTriageMessageRequest request, CancellationToken cancellationToken = default);
    Task<TriageSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<TriageSessionResponse> CompleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TriageSessionResponse>> GetSessionsByPatientAsync(Guid patientId, int limit, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TriageSessionResponse>> GetSessionsByPatientsAsync(
        IReadOnlyList<Guid> patientIds,
        int limit,
        CancellationToken cancellationToken = default);
}

public interface ITriageEventPublisher
{
    /// <returns>Decision summary when Kafka off and HTTP routing succeeded; otherwise null (Kafka path).</returns>
    Task<RoutingDecisionSummaryDto?> PublishTriageCompletedAsync(
        Guid sessionId,
        Guid patientId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default);
}

/// <summary>HTTP fallback when Kafka выключен: синхронно создаёт routing decision.</summary>
public interface IRoutingDispatchClient
{
    Task<RoutingDecisionSummaryDto?> DispatchTriageCompletedAsync(
        Guid sessionId,
        Guid patientId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default);
}
