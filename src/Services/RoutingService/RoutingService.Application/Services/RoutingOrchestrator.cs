using System.Text.Json;
using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using RoutingService.Domain.Entities;
using RoutingService.Domain.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RoutingService.Application.Services;

public sealed class RoutingOrchestrator : IRoutingOrchestrator
{
    private readonly IRoutingEngine _engine;
    private readonly IMedicalRecordContextClient _medicalRecord;
    private readonly IDoctorScheduler _scheduler;
    private readonly IRoutingDecisionRepository _decisions;
    private readonly IPatientRouteRepository _routes;
    private readonly IRoutingEventPublisher _publisher;
    private readonly KafkaOptions _kafka;
    private readonly RoutingEngineOptions _engineOptions;
    private readonly ILogger<RoutingOrchestrator> _logger;

    public RoutingOrchestrator(
        IRoutingEngine engine,
        IMedicalRecordContextClient medicalRecord,
        IDoctorScheduler scheduler,
        IRoutingDecisionRepository decisions,
        IPatientRouteRepository routes,
        IRoutingEventPublisher publisher,
        IOptions<KafkaOptions> kafka,
        IOptions<RoutingEngineOptions> engineOptions,
        ILogger<RoutingOrchestrator> logger)
    {
        _engine = engine;
        _medicalRecord = medicalRecord;
        _scheduler = scheduler;
        _decisions = decisions;
        _routes = routes;
        _publisher = publisher;
        _kafka = kafka.Value;
        _engineOptions = engineOptions.Value;
        _logger = logger;
    }

    public async Task<RoutingDecisionResponse> ProcessTriageCompletedAsync(
        TriageCompletedEventDto triageEvent,
        CancellationToken cancellationToken = default)
    {
        var medicalContext = await _medicalRecord.GetContextAsync(triageEvent.PatientId, cancellationToken);
        var clinicRules = await LoadClinicRulesAsync(cancellationToken);

        var engineInput = new RoutingEngineInput
        {
            TriageEvent = triageEvent,
            MedicalContext = medicalContext,
            ClinicRules = clinicRules
        };

        var engineResult = _engine.Evaluate(engineInput);

        DoctorSlotDto? doctor = null;
        if (engineResult.OutcomeType is nameof(RoutingOutcomeType.Consultation) or nameof(RoutingOutcomeType.LabsBeforeConsultation))
        {
            doctor = await _scheduler.FindAvailableDoctorAsync(
                engineResult.Specialist ?? _engineOptions.DefaultSpecialist,
                engineResult.EffectiveUrgencyLevel,
                cancellationToken);

            if (doctor is null)
            {
                engineResult = ApplyDoctorUnavailableFallback(engineResult);
                doctor = await _scheduler.FindAvailableDoctorAsync(
                    _engineOptions.DefaultSpecialist,
                    engineResult.EffectiveUrgencyLevel,
                    cancellationToken);
            }
        }

        var decisionId = Guid.NewGuid();
        var publishedEvents = new List<string>();

        await PublishOutcomeEventsAsync(triageEvent, engineResult, doctor, publishedEvents, cancellationToken);

        var decision = new RoutingDecision
        {
            Id = decisionId,
            PatientId = triageEvent.PatientId,
            TriageSessionId = triageEvent.SessionId,
            OutcomeType = ParseOutcomeType(engineResult.OutcomeType),
            Specialist = engineResult.Specialist,
            ConsultationFormat = ParseConsultationFormat(engineResult.ConsultationFormat),
            AssignedDoctorId = doctor?.DoctorId,
            AssignedDoctorName = doctor?.FullName,
            Priority = engineResult.Priority,
            UrgencyLevel = engineResult.EffectiveUrgencyLevel,
            RecommendedLabsJson = JsonSerializer.Serialize(engineResult.RecommendedLabs),
            PatientMessage = engineResult.PatientMessage,
            Rationale = engineResult.Rationale,
            AlgorithmVersion = _engineOptions.AlgorithmVersion,
            IsFallback = engineResult.IsFallback,
            CreatedAt = DateTime.UtcNow
        };

        var audit = new RoutingAuditEntry
        {
            Id = Guid.NewGuid(),
            DecisionId = decisionId,
            InputEventJson = JsonSerializer.Serialize(triageEvent),
            MedicalContextJson = JsonSerializer.Serialize(medicalContext),
            RejectedAlternativesJson = JsonSerializer.Serialize(engineResult.RejectedAlternatives),
            PublishedEventsJson = JsonSerializer.Serialize(publishedEvents),
            AlgorithmVersion = _engineOptions.AlgorithmVersion,
            CreatedAt = DateTime.UtcNow
        };

        await _decisions.SaveDecisionWithAuditAsync(decision, audit, cancellationToken);
        await UpsertPatientRouteAsync(triageEvent.PatientId, decisionId, engineResult, cancellationToken);

        _logger.LogInformation(
            "Routing decision {DecisionId} for patient {PatientId}: outcome={Outcome} specialist={Specialist}",
            decisionId,
            triageEvent.PatientId,
            engineResult.OutcomeType,
            engineResult.Specialist);

        return MapResponse(decision, engineResult.RecommendedLabs, publishedEvents);
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadClinicRulesAsync(CancellationToken cancellationToken)
    {
        var rules = await _decisions.GetActiveRulesAsync("default", cancellationToken);
        return rules.ToDictionary(r => r.RuleKey, r => r.RuleValue, StringComparer.OrdinalIgnoreCase);
    }

    private async Task PublishOutcomeEventsAsync(
        TriageCompletedEventDto triageEvent,
        RoutingEngineResult result,
        DoctorSlotDto? doctor,
        List<string> publishedEvents,
        CancellationToken cancellationToken)
    {
        foreach (var eventName in result.EventsToPublish)
        {
            var topic = eventName switch
            {
                "routing.decision" => _kafka.RoutingDecisionTopic,
                "lab.order_required" => _kafka.LabOrderRequiredTopic,
                "emergency_required" => _kafka.EmergencyRequiredTopic,
                "auto_response_required" => _kafka.AutoResponseRequiredTopic,
                _ => eventName
            };

            var payload = eventName switch
            {
                "routing.decision" => new
                {
                    triageEvent.PatientId,
                    triageEvent.SessionId,
                    result.OutcomeType,
                    result.Specialist,
                    result.ConsultationFormat,
                    DoctorId = doctor?.DoctorId,
                    DoctorName = doctor?.FullName,
                    result.Priority,
                    result.EffectiveUrgencyLevel,
                    result.RecommendedLabs,
                    result.PatientMessage,
                    result.Rationale
                },
                "lab.order_required" => new
                {
                    triageEvent.PatientId,
                    triageEvent.SessionId,
                    Labs = result.RecommendedLabs,
                    Priority = result.EffectiveUrgencyLevel >= 4 ? "urgent" : "planned",
                    PreferredLaboratory = "default"
                },
                "emergency_required" => new
                {
                    triageEvent.PatientId,
                    triageEvent.SessionId,
                    Symptoms = triageEvent.PatientMessageSummary,
                    triageEvent.Hypotheses,
                    triageEvent.UrgencyLevel
                },
                "auto_response_required" => new
                {
                    triageEvent.PatientId,
                    triageEvent.SessionId,
                    Question = triageEvent.PatientMessageSummary,
                    Category = "general"
                },
                _ => (object)new { triageEvent.PatientId, triageEvent.SessionId }
            };

            await _publisher.PublishAsync(topic, payload, cancellationToken);
            publishedEvents.Add(eventName);
        }
    }

    private async Task UpsertPatientRouteAsync(
        Guid patientId,
        Guid decisionId,
        RoutingEngineResult result,
        CancellationToken cancellationToken)
    {
        if (result.PlannedSteps is null || result.PlannedSteps.Count <= 1)
            return;

        var existing = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
        var route = existing ?? new PatientRoute
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            CreatedAt = DateTime.UtcNow
        };

        route.CurrentDecisionId = decisionId;
        route.Status = "active";
        route.CurrentStep = 1;
        route.TotalSteps = result.PlannedSteps.Count;
        route.StepsJson = JsonSerializer.Serialize(result.PlannedSteps);
        route.UpdatedAt = DateTime.UtcNow;

        await _routes.SaveAsync(route, cancellationToken);
    }

    private RoutingEngineResult ApplyDoctorUnavailableFallback(RoutingEngineResult result)
    {
        var alternatives = result.RejectedAlternatives.ToList();
        alternatives.Add(new RejectedAlternativeDto
        {
            Option = result.Specialist ?? "specialist",
            Reason = "No available doctors for requested specialty"
        });

        return new RoutingEngineResult
        {
            OutcomeType = result.OutcomeType,
            Specialist = _engineOptions.DefaultSpecialist,
            ConsultationFormat = result.ConsultationFormat,
            Priority = result.Priority,
            EffectiveUrgencyLevel = result.EffectiveUrgencyLevel,
            RecommendedLabs = result.RecommendedLabs,
            PatientMessage = result.PatientMessage,
            Rationale = result.Rationale + " Fallback: assigned general therapist due to specialist unavailability.",
            IsFallback = true,
            RejectedAlternatives = alternatives,
            EventsToPublish = result.EventsToPublish,
            PlannedSteps = result.PlannedSteps
        };
    }

    private static RoutingOutcomeType ParseOutcomeType(string outcomeType) =>
        Enum.TryParse<RoutingOutcomeType>(outcomeType, out var parsed) ? parsed : RoutingOutcomeType.Consultation;

    private static ConsultationFormat? ParseConsultationFormat(string? format) =>
        format is not null && Enum.TryParse<ConsultationFormat>(format, out var parsed) ? parsed : null;

    private static RoutingDecisionResponse MapResponse(
        RoutingDecision decision,
        IReadOnlyList<string> labs,
        IReadOnlyList<string> publishedEvents) => new()
    {
        DecisionId = decision.Id,
        PatientId = decision.PatientId,
        TriageSessionId = decision.TriageSessionId,
        OutcomeType = decision.OutcomeType.ToString(),
        Specialist = decision.Specialist,
        ConsultationFormat = decision.ConsultationFormat?.ToString(),
        AssignedDoctorId = decision.AssignedDoctorId,
        AssignedDoctorName = decision.AssignedDoctorName,
        Priority = decision.Priority,
        UrgencyLevel = decision.UrgencyLevel,
        RecommendedLabs = labs,
        PatientMessage = decision.PatientMessage,
        Rationale = decision.Rationale,
        AlgorithmVersion = decision.AlgorithmVersion,
        IsFallback = decision.IsFallback,
        PublishedEvents = publishedEvents
    };
}
