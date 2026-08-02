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
    private readonly IConsultationDispatchClient _consultations;
    private readonly ILabOrderDispatchClient _labOrders;
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
        IConsultationDispatchClient consultations,
        ILabOrderDispatchClient labOrders,
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
        _consultations = consultations;
        _labOrders = labOrders;
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

        Guid? consultationSessionId = null;
        if (!_kafka.Enabled)
        {
            consultationSessionId = await DispatchHttpSideEffectsAsync(
                decisionId,
                triageEvent,
                engineResult,
                doctor,
                cancellationToken);
        }
        else if (engineResult.OutcomeType == nameof(RoutingOutcomeType.LabsBeforeConsultation) &&
                 engineResult.RecommendedLabs.Count > 0)
        {
            // Lab orders не создаёт Integration stub — всегда пишем в PrescriptionService.
            await _labOrders.CreateFromRoutingAsync(
                triageEvent.PatientId,
                doctor?.DoctorId,
                consultationId: null,
                engineResult.RecommendedLabs,
                engineResult.EffectiveUrgencyLevel >= 4 ? "urgent" : "routine",
                cancellationToken);
        }

        _logger.LogInformation(
            "Routing decision {DecisionId} for patient {PatientId}: outcome={Outcome} specialist={Specialist}",
            decisionId,
            triageEvent.PatientId,
            engineResult.OutcomeType,
            engineResult.Specialist);

        return MapResponse(decision, engineResult.RecommendedLabs, publishedEvents, consultationSessionId);
    }

    private async Task<Guid?> DispatchHttpSideEffectsAsync(
        Guid decisionId,
        TriageCompletedEventDto triageEvent,
        RoutingEngineResult engineResult,
        DoctorSlotDto? doctor,
        CancellationToken cancellationToken)
    {
        Guid? consultationSessionId = null;

        if (doctor is not null &&
            engineResult.OutcomeType is nameof(RoutingOutcomeType.Consultation)
                or nameof(RoutingOutcomeType.LabsBeforeConsultation))
        {
            consultationSessionId = await _consultations.CreateFromRoutingDecisionAsync(
                decisionId,
                triageEvent,
                engineResult,
                doctor,
                cancellationToken);
        }

        if (engineResult.OutcomeType == nameof(RoutingOutcomeType.LabsBeforeConsultation) &&
            engineResult.RecommendedLabs.Count > 0)
        {
            await _labOrders.CreateFromRoutingAsync(
                triageEvent.PatientId,
                doctor?.DoctorId,
                consultationSessionId,
                engineResult.RecommendedLabs,
                engineResult.EffectiveUrgencyLevel >= 4 ? "urgent" : "routine",
                cancellationToken);
        }

        return consultationSessionId;
    }

    public async Task<PatientActiveRouteResponse?> GetActiveRouteAsync(
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var route = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
        if (route is null)
        {
            // Heal: раньше route писался только для LabsBeforeConsultation — поднимаем из последнего decision.
            var latest = await _decisions.GetLatestByPatientIdAsync(patientId, cancellationToken);
            if (latest is null)
                return null;

            var labs = JsonSerializer.Deserialize<List<string>>(latest.RecommendedLabsJson) ?? [];
            var steps = BuildStepsFromDecision(latest, labs);
            route = new PatientRoute
            {
                Id = Guid.NewGuid(),
                PatientId = patientId,
                CurrentDecisionId = latest.Id,
                Status = "active",
                CurrentStep = 1,
                TotalSteps = steps.Count,
                StepsJson = JsonSerializer.Serialize(steps),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _routes.SaveAsync(route, cancellationToken);
        }

        return MapRoute(route);
    }

    public async Task<PatientActiveRouteResponse> AppendPostConsultationLabsAsync(
        Guid patientId,
        AppendPostConsultationLabsRequest request,
        CancellationToken cancellationToken = default)
    {
        var labs = (request.Labs ?? Array.Empty<string>())
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (labs.Count == 0)
            throw new InvalidOperationException("Нужен хотя бы один анализ.");

        var route = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
        if (route is null)
        {
            await GetActiveRouteAsync(patientId, cancellationToken);
            route = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
        }

        route ??= new PatientRoute
        {
            Id = Guid.NewGuid(),
            PatientId = patientId,
            Status = "active",
            CurrentStep = 1,
            CreatedAt = DateTime.UtcNow,
            StepsJson = "[]"
        };

        var steps = JsonSerializer.Deserialize<List<RouteStepDto>>(route.StepsJson) ?? [];
        var labStep = steps.FirstOrDefault(s =>
            string.Equals(s.Action, "lab.order", StringComparison.OrdinalIgnoreCase));

        var description = $"Назначены анализы: {string.Join(", ", labs)}";
        if (labStep is null)
        {
            var nextNumber = steps.Count == 0 ? 1 : steps.Max(s => s.StepNumber) + 1;
            steps.Add(new RouteStepDto
            {
                StepNumber = nextNumber,
                Action = "lab.order",
                Status = "pending",
                Description = description
            });
        }
        else
        {
            var existing = ParseLabsFromStepDescription(labStep.Description);
            var merged = existing.Concat(labs).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            labStep.Description = $"Назначены анализы: {string.Join(", ", merged)}";
            labStep.Status = "pending";
        }

        foreach (var step in steps.Where(s =>
                     string.Equals(s.Action, "consultation", StringComparison.OrdinalIgnoreCase)))
        {
            if (step.Status is "pending" or "waiting_for_labs")
                step.Status = "completed";
        }

        route.StepsJson = JsonSerializer.Serialize(steps);
        route.TotalSteps = steps.Count;
        var labIndex = steps.FindIndex(s =>
            string.Equals(s.Action, "lab.order", StringComparison.OrdinalIgnoreCase));
        route.CurrentStep = labIndex >= 0 ? labIndex + 1 : steps.Count;
        route.Status = "active";
        route.UpdatedAt = DateTime.UtcNow;
        await _routes.SaveAsync(route, cancellationToken);

        await MergeLabsIntoDecisionAsync(patientId, route.CurrentDecisionId, labs, cancellationToken);

        _logger.LogInformation(
            "Appended post-consultation labs for patient {PatientId} consultation {ConsultationId}: {Labs}",
            patientId,
            request.ConsultationId,
            string.Join(", ", labs));

        return MapRoute(route);
    }

    private async Task MergeLabsIntoDecisionAsync(
        Guid patientId,
        Guid? decisionId,
        IReadOnlyList<string> labs,
        CancellationToken cancellationToken)
    {
        RoutingDecision? decision = null;
        if (decisionId is Guid id)
            decision = await _decisions.GetByIdAsync(id, cancellationToken);

        decision ??= await _decisions.GetLatestByPatientIdAsync(patientId, cancellationToken);
        if (decision is null)
            return;

        var existing = JsonSerializer.Deserialize<List<string>>(decision.RecommendedLabsJson) ?? [];
        var merged = existing.Concat(labs).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        decision.RecommendedLabsJson = JsonSerializer.Serialize(merged);
        await _decisions.UpdateAsync(decision, cancellationToken);

        if (decisionId is null)
        {
            var route = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
            if (route is not null && route.CurrentDecisionId is null)
            {
                route.CurrentDecisionId = decision.Id;
                route.UpdatedAt = DateTime.UtcNow;
                await _routes.SaveAsync(route, cancellationToken);
            }
        }
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
        var steps = result.PlannedSteps is { Count: > 0 }
            ? result.PlannedSteps
            : new[]
            {
                new RouteStepDto
                {
                    StepNumber = 1,
                    Action = MapAction(result.OutcomeType),
                    Status = "pending",
                    Description = string.IsNullOrWhiteSpace(result.PatientMessage)
                        ? result.OutcomeType
                        : result.PatientMessage
                }
            };

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
        route.TotalSteps = steps.Count;
        route.StepsJson = JsonSerializer.Serialize(steps);
        route.UpdatedAt = DateTime.UtcNow;

        await _routes.SaveAsync(route, cancellationToken);
    }

    private static PatientActiveRouteResponse MapRoute(PatientRoute route)
    {
        var steps = JsonSerializer.Deserialize<List<RouteStepDto>>(route.StepsJson) ?? [];
        return new PatientActiveRouteResponse
        {
            RouteId = route.Id,
            PatientId = route.PatientId,
            CurrentDecisionId = route.CurrentDecisionId,
            Status = route.Status,
            CurrentStep = route.CurrentStep,
            TotalSteps = route.TotalSteps,
            Steps = steps
        };
    }

    private static List<RouteStepDto> BuildStepsFromDecision(RoutingDecision decision, IReadOnlyList<string> labs)
    {
        if (labs.Count > 0)
        {
            return
            [
                new RouteStepDto
                {
                    StepNumber = 1,
                    Action = "lab.order",
                    Status = "pending",
                    Description = $"Назначены анализы: {string.Join(", ", labs)}"
                },
                new RouteStepDto
                {
                    StepNumber = 2,
                    Action = "consultation",
                    Status = "waiting_for_labs",
                    Description = string.IsNullOrWhiteSpace(decision.PatientMessage)
                        ? $"Консультация {decision.Specialist ?? "врача"}"
                        : decision.PatientMessage
                }
            ];
        }

        return
        [
            new RouteStepDto
            {
                StepNumber = 1,
                Action = MapAction(decision.OutcomeType.ToString()),
                Status = "pending",
                Description = string.IsNullOrWhiteSpace(decision.PatientMessage)
                    ? decision.OutcomeType.ToString()
                    : decision.PatientMessage
            }
        ];
    }

    private static IReadOnlyList<string> ParseLabsFromStepDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
            return Array.Empty<string>();

        const string prefix = "Назначены анализы:";
        var idx = description.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return Array.Empty<string>();

        return description[(idx + prefix.Length)..]
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string MapAction(string outcomeType) => outcomeType switch
    {
        nameof(RoutingOutcomeType.LabsBeforeConsultation) => "lab.order",
        nameof(RoutingOutcomeType.Emergency) => "emergency",
        nameof(RoutingOutcomeType.HomeVisit) => "home.visit",
        nameof(RoutingOutcomeType.AutoResponse) => "auto.response",
        _ => "consultation"
    };

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
        IReadOnlyList<string> publishedEvents,
        Guid? consultationSessionId = null) => new()
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
        ConsultationSessionId = consultationSessionId,
        IsFallback = decision.IsFallback,
        PublishedEvents = publishedEvents
    };
}
