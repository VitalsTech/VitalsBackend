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
}

public interface ITriageOrchestrator
{
    Task<TriageSessionResponse> CreateSessionAsync(CreateTriageSessionRequest request, CancellationToken cancellationToken = default);
    Task<TriageSessionResponse> ProcessMessageAsync(Guid sessionId, SendTriageMessageRequest request, CancellationToken cancellationToken = default);
    Task<TriageSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default);
}

public interface ITriageEventPublisher
{
    Task PublishTriageCompletedAsync(Guid sessionId, Guid patientId, LlmTriageResultDto result, CancellationToken cancellationToken = default);
}
