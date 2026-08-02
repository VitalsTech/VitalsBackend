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

        var assistantText = BuildAssistantReply(llmResult);

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

        // Маршрутизация только на complete — не на каждое сообщение.
        session = await _sessions.GetByIdAsync(sessionId, cancellationToken) ?? session;
        return MapSession(session);
    }

    /// <summary>
    /// Завершает триаж и запускает routing. Оценка — из последнего LLM-ответа
    /// или финальный вызов модели по истории диалога (без mock).
    /// </summary>
    public async Task<TriageSessionResponse> CompleteSessionAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        var session = await _sessions.GetByIdAsync(sessionId, cancellationToken)
            ?? throw new TriageNotFoundException(sessionId);

        var latestAssessment = session.Assessments.OrderByDescending(a => a.CreatedAt).FirstOrDefault();
        LlmTriageResultDto llmResult;

        if (latestAssessment is null)
        {
            var history = session.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => new TriageMessageDto { Role = m.Role, Content = m.Content, CreatedAt = m.CreatedAt })
                .ToList();

            var lastPatient = history.LastOrDefault(m =>
                string.Equals(m.Role, "Patient", StringComparison.OrdinalIgnoreCase));

            var medicalContext = await _medicalRecord.GetContextAsync(session.PatientId, cancellationToken);
            llmResult = await _llm.AnalyzeAsync(new TriagePromptContext
            {
                SessionId = sessionId,
                PatientId = session.PatientId,
                CurrentMessage = lastPatient?.Content ?? "Пациент завершил триаж. Сформируй итоговую оценку по имеющемуся диалогу.",
                DialogHistory = history,
                ParsedEntities = Array.Empty<ExtractedEntityDto>(),
                NerEntities = Array.Empty<NerEntityDto>(),
                MedicalContext = medicalContext
            }, cancellationToken);

            session.LatestUrgencyLevel = llmResult.UrgencyLevel;
            var finalAssessment = new TriageAssessment
            {
                SessionId = sessionId,
                MessageId = session.Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault()?.Id ?? Guid.Empty,
                UrgencyLevel = llmResult.UrgencyLevel,
                ExtractedEntitiesJson = "[]",
                NerEntitiesJson = "[]",
                LlmResultJson = JsonSerializer.Serialize(llmResult),
                AssistantReply = llmResult.RecommendedAction
            };
            await _sessions.AddAssessmentAsync(finalAssessment, cancellationToken);
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

        // Kafka (если включён) или sync HTTP в Routing внутри publisher — иначе active-route пустой.
        var decision = await _publisher.PublishTriageCompletedAsync(sessionId, session.PatientId, llmResult, cancellationToken);

        await _medicalRecord.AppendTriageCompletedEventAsync(session.PatientId, sessionId, llmResult, cancellationToken);

        _logger.LogInformation("Triage session {SessionId} completed for patient {PatientId}", sessionId, session.PatientId);
        var response = MapSession(session);
        if (decision is not null)
        {
            response.RoutingDecisionId = decision.DecisionId;
            response.RoutingOutcomeType = decision.OutcomeType;
            response.AssignedDoctorId = decision.AssignedDoctorId;
            response.AssignedDoctorName = decision.AssignedDoctorName;
            response.RecommendedLabs = decision.RecommendedLabs ?? Array.Empty<string>();
            response.ConsultationSessionId = decision.ConsultationSessionId;
            if (!string.IsNullOrWhiteSpace(decision.Specialist))
                response.RecommendedSpecialization = decision.Specialist;
            if (!string.IsNullOrWhiteSpace(decision.PatientMessage))
            {
                response.Recommendation = decision.PatientMessage;
                response.RecommendationText = decision.PatientMessage;
            }
        }

        return response;
    }

    public async Task<IReadOnlyList<TriageSessionResponse>> GetSessionsByPatientAsync(
        Guid patientId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await GetSessionsByPatientsAsync(new[] { patientId }, limit, cancellationToken);

    public async Task<IReadOnlyList<TriageSessionResponse>> GetSessionsByPatientsAsync(
        IReadOnlyList<Guid> patientIds,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _sessions.GetByPatientIdsAsync(patientIds, limit, cancellationToken);
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
        var llm = latestAssessment is null
            ? null
            : JsonSerializer.Deserialize<LlmTriageResultDto>(latestAssessment.LlmResultJson);

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
            Recommendation = llm?.RecommendedAction ?? latestAssessment?.AssistantReply,
            RecommendationText = llm?.RecommendedAction ?? latestAssessment?.AssistantReply ?? BuildRecommendation(latestAssessment),
            RecommendedSpecialization = "Терапевт",
            CanBeRemote = session.LatestUrgencyLevel <= 3,
            ReadyToComplete = llm?.ReadyToComplete == true &&
                              !string.Equals(session.Status, "Completed", StringComparison.OrdinalIgnoreCase),
            CompleteSuggestion = llm?.ReadyToComplete == true ? llm.CompleteSuggestion : null
        };
    }

    private static string BuildAssistantReply(LlmTriageResultDto llmResult)
    {
        const string disclaimer =
            "Важно: это предварительная оценка, а не диагноз. Назначение лекарств возможно только врачом.";

        if (llmResult.EmergencyWarning)
        {
            var text = llmResult.RecommendedAction;
            if (!string.IsNullOrWhiteSpace(llmResult.CompleteSuggestion))
                text += "\n\n" + llmResult.CompleteSuggestion;
            else if (!string.IsNullOrWhiteSpace(llmResult.NextQuestion))
                text += "\n\n" + llmResult.NextQuestion;
            return text;
        }

        if (llmResult.ReadyToComplete)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(llmResult.CompleteSuggestion))
                parts.Add(llmResult.CompleteSuggestion!);
            parts.Add($"Рекомендация: {llmResult.RecommendedAction}");
            parts.Add("Когда будете готовы — нажмите «Завершить триаж».");
            parts.Add(disclaimer);
            return string.Join("\n\n", parts);
        }

        return $"{llmResult.NextQuestion}\n\nРекомендация: {llmResult.RecommendedAction}\n\n{disclaimer}";
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
