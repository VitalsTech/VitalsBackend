using System.Text.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Exceptions;
using AITriageService.Application.Interfaces;
using AITriageService.Domain.Entities;
using AITriageService.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace AITriageService.Application.Services;

public sealed class TriageOrchestrator : ITriageOrchestrator
{
    private readonly ITriageSessionRepository _sessions;
    private readonly ISymptomParser _parser;
    private readonly INerService _ner;
    private readonly ILlmTriageService _llm;
    private readonly IMedicalRecordContextClient _medicalRecord;
    private readonly ITriageEventPublisher _publisher;
    private readonly ILogger<TriageOrchestrator> _logger;

    public TriageOrchestrator(
        ITriageSessionRepository sessions,
        ISymptomParser parser,
        INerService ner,
        ILlmTriageService llm,
        IMedicalRecordContextClient medicalRecord,
        ITriageEventPublisher publisher,
        ILogger<TriageOrchestrator> logger)
    {
        _sessions = sessions;
        _parser = parser;
        _ner = ner;
        _llm = llm;
        _medicalRecord = medicalRecord;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task<TriageSessionResponse> CreateSessionAsync(CreateTriageSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = new TriageSession
        {
            PatientId = request.PatientId,
            CorrelationId = request.CorrelationId
        };

        await _sessions.AddAsync(session, cancellationToken);
        await _sessions.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Triage session {SessionId} created for patient {PatientId}", session.Id, session.PatientId);
        return MapSession(session);
    }

    public async Task<TriageSessionResponse> ProcessMessageAsync(
        Guid sessionId,
        SendTriageMessageRequest request,
        CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new TriageNotFoundException(sessionId);

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new TriageValidationException("Message cannot be empty.");

        var patientMessage = new TriageMessage
        {
            SessionId = sessionId,
            Role = "Patient",
            Content = request.Message.Trim()
        };
        await _sessions.AddMessageAsync(patientMessage, cancellationToken);

        var parsed = _parser.Parse(patientMessage.Content);
        var ner = await _ner.ExtractAsync(patientMessage.Content, parsed, cancellationToken);
        var medicalContext = await _medicalRecord.GetContextAsync(session.PatientId, cancellationToken);

        var history = session.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(m => new TriageMessageDto { Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt })
            .ToList();

        var llmResult = await _llm.AnalyzeAsync(new TriagePromptContext
        {
            SessionId = sessionId,
            PatientId = session.PatientId,
            CurrentMessage = patientMessage.Content,
            DialogHistory = history,
            ParsedEntities = parsed,
            NerEntities = ner,
            MedicalContext = medicalContext
        }, cancellationToken);

        var assistantText = llmResult.EmergencyWarning
            ? $"{llmResult.RecommendedAction}\n\n{llmResult.NextQuestion}"
            : $"{llmResult.NextQuestion}\n\nРекомендация: {llmResult.RecommendedAction}\n\nВажно: это предварительная оценка, а не диагноз. Назначение лекарств возможно только врачом.";

        var assistantMessage = new TriageMessage
        {
            SessionId = sessionId,
            Role = "Assistant",
            Content = assistantText
        };
        await _sessions.AddMessageAsync(assistantMessage, cancellationToken);

        var assessment = new TriageAssessment
        {
            SessionId = sessionId,
            MessageId = patientMessage.Id,
            UrgencyLevel = llmResult.UrgencyLevel,
            ExtractedEntitiesJson = JsonSerializer.Serialize(parsed),
            NerEntitiesJson = JsonSerializer.Serialize(ner),
            LlmResultJson = JsonSerializer.Serialize(llmResult),
            AssistantReply = assistantText
        };
        await _sessions.AddAssessmentAsync(assessment, cancellationToken);

        session.LatestUrgencyLevel = llmResult.UrgencyLevel;
        session.UpdatedAt = DateTime.UtcNow;
        await _sessions.SaveChangesAsync(cancellationToken);

        await _publisher.PublishTriageCompletedAsync(sessionId, session.PatientId, llmResult, cancellationToken);

        session = await _sessions.GetByIdAsync(sessionId, cancellationToken) ?? session;
        return MapSession(session);
    }

    /// <summary>
    /// Marks triage as completed. If no LLM assessment exists yet, uses mock routing data
    /// (see docs/AITriageService.md — «Завершение триажа (mock)»).
    /// </summary>
    public async Task<TriageSessionResponse> CompleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new TriageNotFoundException(sessionId);

        var latestAssessment = session.Assessments.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        LlmTriageResultDto llmResult;

        if (latestAssessment is null)
        {
            llmResult = new LlmTriageResultDto
            {
                UrgencyLevel = 2,
                RecommendedAction = "Запись к терапевту в течение 3 дней. При ухудшении — срочная консультация.",
                NextQuestion = string.Empty,
                EmergencyWarning = false
            };
            session.LatestUrgencyLevel = llmResult.UrgencyLevel;

            var mockAssessment = new TriageAssessment
            {
                SessionId = sessionId,
                MessageId = session.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Id ?? Guid.Empty,
                UrgencyLevel = llmResult.UrgencyLevel,
                ExtractedEntitiesJson = "[]",
                NerEntitiesJson = "[]",
                LlmResultJson = JsonSerializer.Serialize(llmResult),
                AssistantReply = llmResult.RecommendedAction
            };
            await _sessions.AddAssessmentAsync(mockAssessment, cancellationToken);
        }
        else
        {
            llmResult = JsonSerializer.Deserialize<LlmTriageResultDto>(latestAssessment.LlmResultJson) ?? new LlmTriageResultDto
            {
                UrgencyLevel = latestAssessment.UrgencyLevel,
                RecommendedAction = latestAssessment.AssistantReply
            };
            session.LatestUrgencyLevel = llmResult.UrgencyLevel;
        }

        session.Status = "Completed";
        session.UpdatedAt = DateTime.UtcNow;
        await _sessions.SaveChangesAsync(cancellationToken);

        await _publisher.PublishTriageCompletedAsync(sessionId, session.PatientId, llmResult, cancellationToken);
        await _medicalRecord.AppendTriageCompletedEventAsync(session.PatientId, sessionId, llmResult, cancellationToken);

        _logger.LogInformation("Triage session {SessionId} completed for patient {PatientId}", sessionId, session.PatientId);
        return MapSession(session);
    }

    public async Task<IReadOnlyList<TriageSessionResponse>> GetSessionsByPatientAsync(
        Guid patientId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessions.GetByPatientIdAsync(patientId, limit, cancellationToken);
        return sessions.Select(MapSession).ToList();
    }

    public async Task<TriageSessionResponse> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new TriageNotFoundException(sessionId);
        return MapSession(session);
    }

    private static TriageSessionResponse MapSession(TriageSession session)
    {
        var latestAssessment = session.Assessments.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        return new TriageSessionResponse
        {
            SessionId = session.Id,
            PatientId = session.PatientId,
            Status = session.Status,
            LatestUrgencyLevel = session.LatestUrgencyLevel,
            CreatedAt = session.CreatedAt,
            Messages = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TriageMessageDto { Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt })
                .ToList(),
            LatestAssessment = latestAssessment is null ? null : MapAssessment(latestAssessment),
            Urgency = MapUrgencyLabel(session.LatestUrgencyLevel),
            Recommendation = latestAssessment?.AssistantReply,
            RecommendationText = latestAssessment?.AssistantReply ?? BuildRecommendation(latestAssessment),
            RecommendedSpecialization = "Терапевт",
            CanBeRemote = session.LatestUrgencyLevel <= 3
        };
    }

    private static string MapUrgencyLabel(int level) => level switch
    {
        >= 5 => "emergency",
        >= 4 => "urgent",
        _ => "routine"
    };

    private static string? BuildRecommendation(TriageAssessment? assessment)
    {
        if (assessment is null)
            return null;

        var llm = JsonSerializer.Deserialize<LlmTriageResultDto>(assessment.LlmResultJson);
        return llm?.RecommendedAction;
    }

    private static TriageAssessmentDto MapAssessment(TriageAssessment assessment) => new()
    {
        UrgencyLevel = assessment.UrgencyLevel,
        ExtractedEntities = JsonSerializer.Deserialize<List<ExtractedEntityDto>>(assessment.ExtractedEntitiesJson) ?? new(),
        NerEntities = JsonSerializer.Deserialize<List<NerEntityDto>>(assessment.NerEntitiesJson) ?? new(),
        LlmResult = JsonSerializer.Deserialize<LlmTriageResultDto>(assessment.LlmResultJson) ?? new(),
        AssistantReply = assessment.AssistantReply
    };
}
