using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using RoutingService.Domain.Enums;
using Microsoft.Extensions.Options;

namespace RoutingService.Infrastructure.Routing;

public sealed class RuleBasedRoutingEngine : IRoutingEngine
{
    private readonly RoutingEngineOptions _options;

    private static readonly Dictionary<string, string> ConditionSpecialtyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        ["орви"] = "therapist",
        ["грипп"] = "therapist",
        ["простуда"] = "therapist",
        ["насморк"] = "therapist",
        ["кашель"] = "therapist",
        ["аллерг"] = "allergist",
        ["ишем"] = "cardiologist",
        ["карди"] = "cardiologist",
        ["груд"] = "cardiologist",
        ["диабет"] = "endocrinologist",
        ["гастро"] = "gastroenterologist",
        ["живот"] = "gastroenterologist",
        ["неврол"] = "neurologist",
        ["голов"] = "neurologist",
        ["псих"] = "psychiatrist",
        ["сып"] = "dermatologist",
        ["кож"] = "dermatologist"
    };

    private static readonly string[] AutoResponseKeywords =
    [
        "норма температур",
        "как хранить",
        "обратная связь",
        "спасибо за сервис",
        "сколько градусов считается"
    ];

    private static readonly string[] PhysicalExamKeywords =
    [
        "прослушать легк",
        "экг",
        "измерить давление",
        "осмотр на месте"
    ];

    private static readonly string[] VideoRequiredKeywords =
    [
        "сып",
        "отек",
        "сустав",
        "зев",
        "дыхание",
        "одыш"
    ];

    public RuleBasedRoutingEngine(IOptions<RoutingEngineOptions> options)
    {
        _options = options.Value;
    }

    public RoutingEngineResult Evaluate(RoutingEngineInput input)
    {
        var triage = input.TriageEvent;
        var rejected = new List<RejectedAlternativeDto>();
        var effectiveUrgency = NormalizeUrgency(triage, rejected);
        var summary = (triage.PatientMessageSummary ?? string.Empty).ToLowerInvariant();
        var isPediatric = triage.PatientAgeYears is > 0 and var age && age <= _options.PediatricMaxAgeYears;

        if (triage.EmergencyWarning || effectiveUrgency >= 5)
        {
            return BuildEmergency(triage, effectiveUrgency, rejected);
        }

        if (IsAutoResponseCandidate(triage, summary))
        {
            rejected.Add(new RejectedAlternativeDto { Option = "Consultation", Reason = "Informational query without clinical need" });
            return new RoutingEngineResult
            {
                OutcomeType = nameof(RoutingOutcomeType.AutoResponse),
                EffectiveUrgencyLevel = effectiveUrgency,
                Priority = CalculatePriority(effectiveUrgency, input.ClinicRules),
                PatientMessage = "Мы подготовили ответ из базы знаний Vitals.",
                Rationale = "Обращение классифицировано как информационное, врач не требуется.",
                EventsToPublish = ["auto_response_required"]
            };
        }

        var labs = DetectPreConsultationLabs(triage, summary);
        if (labs.Count > 0)
        {
            var specialist = ResolveSpecialist(triage, input.MedicalContext, isPediatric, input.ClinicRules, rejected);
            var steps = BuildLabThenConsultationSteps(specialist, labs);
            return new RoutingEngineResult
            {
                OutcomeType = nameof(RoutingOutcomeType.LabsBeforeConsultation),
                Specialist = specialist,
                ConsultationFormat = nameof(ConsultationFormat.SyncChat),
                EffectiveUrgencyLevel = effectiveUrgency,
                Priority = CalculatePriority(effectiveUrgency, input.ClinicRules) + 5,
                RecommendedLabs = labs,
                PatientMessage = "Сначала сдайте анализы — после результатов мы автоматически назначим консультацию.",
                Rationale = "По протоколу требуются анализы до консультации.",
                EventsToPublish = ["lab.order_required", "routing.decision"],
                PlannedSteps = steps
            };
        }

        if (ShouldScheduleHomeVisit(triage, summary, effectiveUrgency, isPediatric))
        {
            var specialist = isPediatric ? _options.PediatricSpecialist : _options.DefaultSpecialist;
            return new RoutingEngineResult
            {
                OutcomeType = nameof(RoutingOutcomeType.HomeVisit),
                Specialist = specialist,
                ConsultationFormat = nameof(ConsultationFormat.HomeVisit),
                EffectiveUrgencyLevel = effectiveUrgency,
                Priority = CalculatePriority(effectiveUrgency, input.ClinicRules) + 15,
                PatientMessage = "Мы оформим выездную бригаду. Ожидайте звонка диспетчера.",
                Rationale = "Состояние требует очного осмотра, но не экстренной госпитализации.",
                EventsToPublish = ["routing.decision"]
            };
        }

        var selectedSpecialist = ResolveSpecialist(triage, input.MedicalContext, isPediatric, input.ClinicRules, rejected);
        var format = ResolveConsultationFormat(triage, summary, effectiveUrgency, selectedSpecialist, rejected);
        var isFallback = selectedSpecialist == _options.DefaultSpecialist && triage.Hypotheses.Count == 0;

        return new RoutingEngineResult
        {
            OutcomeType = nameof(RoutingOutcomeType.Consultation),
            Specialist = selectedSpecialist,
            ConsultationFormat = format,
            EffectiveUrgencyLevel = effectiveUrgency,
            Priority = CalculatePriority(effectiveUrgency, input.ClinicRules),
            PatientMessage = BuildPatientMessage(selectedSpecialist, format, effectiveUrgency),
            Rationale = BuildRationale(triage, selectedSpecialist, format, input.MedicalContext),
            IsFallback = isFallback,
            RejectedAlternatives = rejected,
            EventsToPublish = ["routing.decision"]
        };
    }

    private int NormalizeUrgency(TriageCompletedEventDto triage, List<RejectedAlternativeDto> rejected)
    {
        if (triage.UrgencyLevel > _options.MaxUrgencyWithoutEmergency && !triage.EmergencyWarning)
        {
            rejected.Add(new RejectedAlternativeDto
            {
                Option = $"urgency-{triage.UrgencyLevel}",
                Reason = "AI urgency capped by safety fallback"
            });
            return 3;
        }

        return triage.UrgencyLevel;
    }

    private static bool IsAutoResponseCandidate(TriageCompletedEventDto triage, string summary)
    {
        if (triage.UrgencyLevel > 1)
            return false;

        if (triage.Hypotheses.Any(h => h.Probability >= 0.5))
            return false;

        return AutoResponseKeywords.Any(summary.Contains);
    }

    private static List<string> DetectPreConsultationLabs(TriageCompletedEventDto triage, string summary)
    {
        var labs = new List<string>();

        if (summary.Contains("горл") && (summary.Contains("недел") || summary.Contains("7 д") || summary.Contains("третий день")))
            labs.Add("throat_swab_culture");

        if (summary.Contains("груд") && triage.UrgencyLevel >= 3)
        {
            labs.Add("troponin");
            labs.Add("lipid_profile");
        }

        return labs;
    }

    private string ResolveSpecialist(
        TriageCompletedEventDto triage,
        PatientMedicalContextDto? context,
        bool isPediatric,
        IReadOnlyDictionary<string, string> clinicRules,
        List<RejectedAlternativeDto> rejected)
    {
        if (isPediatric)
            return _options.PediatricSpecialist;

        if (clinicRules.TryGetValue("force_therapist_for_fever", out var feverThreshold)
            && double.TryParse(feverThreshold, out var temp)
            && (triage.PatientMessageSummary?.Contains(temp.ToString("0.0")) == true
                || triage.PatientMessageSummary?.Contains(temp.ToString("0")) == true))
        {
            rejected.Add(new RejectedAlternativeDto { Option = "narrow_specialist", Reason = "Clinic rule: fever routing to therapist first" });
            return _options.DefaultSpecialist;
        }

        var topHypothesis = triage.Hypotheses.OrderByDescending(h => h.Probability).FirstOrDefault();
        if (topHypothesis is not null)
        {
            foreach (var pair in ConditionSpecialtyMap)
            {
                if (topHypothesis.Condition.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                    return pair.Value;
            }
        }

        if (context?.ActiveDiagnoses.Any(d => d.Contains("E11", StringComparison.OrdinalIgnoreCase) || d.Contains("диабет", StringComparison.OrdinalIgnoreCase)) == true
            && triage.Hypotheses.Any(h => h.Condition.Contains("насморк", StringComparison.OrdinalIgnoreCase)))
        {
            rejected.Add(new RejectedAlternativeDto { Option = "therapist_only", Reason = "Chronic diabetes requires endocrinologist co-management" });
            return "endocrinologist";
        }

        if (triage.Hypotheses.Count == 0)
        {
            rejected.Add(new RejectedAlternativeDto { Option = "specialist_by_hypothesis", Reason = "Insufficient triage data" });
            return _options.DefaultSpecialist;
        }

        return _options.DefaultSpecialist;
    }

    private static string ResolveConsultationFormat(
        TriageCompletedEventDto triage,
        string summary,
        int urgency,
        string specialist,
        List<RejectedAlternativeDto> rejected)
    {
        if (PhysicalExamKeywords.Any(summary.Contains))
        {
            rejected.Add(new RejectedAlternativeDto { Option = "SyncChat", Reason = "Physical examination required" });
            return nameof(ConsultationFormat.InPerson);
        }

        if (urgency >= 4 || VideoRequiredKeywords.Any(summary.Contains) || specialist is "psychiatrist" or "neurologist" or "dermatologist")
            return nameof(ConsultationFormat.Video);

        if (urgency <= 2 && specialist is "endocrinologist" or "therapist")
            return nameof(ConsultationFormat.Async);

        return nameof(ConsultationFormat.SyncChat);
    }

    private static bool ShouldScheduleHomeVisit(TriageCompletedEventDto triage, string summary, int urgency, bool isPediatric)
    {
        if (urgency < 4)
            return false;

        if (isPediatric && summary.Contains("температур"))
            return true;

        return summary.Contains("аппендицит") || summary.Contains("высокая температура") && isPediatric;
    }

    private static RoutingEngineResult BuildEmergency(
        TriageCompletedEventDto triage,
        int urgency,
        List<RejectedAlternativeDto> rejected)
    {
        rejected.Add(new RejectedAlternativeDto { Option = "Consultation", Reason = "Emergency override" });
        return new RoutingEngineResult
        {
            OutcomeType = nameof(RoutingOutcomeType.Emergency),
            EffectiveUrgencyLevel = Math.Max(urgency, 5),
            Priority = 100,
            PatientMessage = "Ваше состояние может быть опасным. Мы передаём данные экстренным службам и отправим инструкции.",
            Rationale = "AI Triage определил признаки экстренного состояния.",
            EventsToPublish = ["emergency_required"]
        };
    }

    private static int CalculatePriority(int urgency, IReadOnlyDictionary<string, string> clinicRules)
    {
        var priority = urgency * 20;
        if (clinicRules.ContainsKey("dms_priority_boost"))
            priority += 10;

        return priority;
    }

    private static string BuildPatientMessage(string specialist, string format, int urgency)
    {
        var wait = urgency >= 4 ? "15 минут" : urgency == 3 ? "24 часа" : "несколько часов";
        return format switch
        {
            nameof(ConsultationFormat.Video) => $"Врач-{MapSpecialtyRu(specialist)} пригласит вас на видеоконсультацию в течение {wait}.",
            nameof(ConsultationFormat.Async) => "Врач ответит в асинхронном чате в течение 24 часов.",
            nameof(ConsultationFormat.InPerson) => "Мы запишем вас на очный приём в ближайшую клинику.",
            _ => $"Врач-{MapSpecialtyRu(specialist)} свяжется с вами в чате в течение {wait}."
        };
    }

    private static string BuildRationale(
        TriageCompletedEventDto triage,
        string specialist,
        string format,
        PatientMedicalContextDto? context)
    {
        var top = triage.Hypotheses.OrderByDescending(h => h.Probability).FirstOrDefault();
        var hypothesisPart = top is null ? "гипотеза не определена" : $"{top.Condition} ({top.Probability:P0})";
        var chronicPart = context?.ActiveDiagnoses.Count > 0
            ? $" Учтены хронические диагнозы: {string.Join(", ", context.ActiveDiagnoses.Take(2))}."
            : string.Empty;

        return $"На основе triage ({hypothesisPart}, urgency={triage.UrgencyLevel}) выбран {specialist}, формат {format}.{chronicPart}";
    }

    private static IReadOnlyList<RouteStepDto> BuildLabThenConsultationSteps(string specialist, IReadOnlyList<string> labs) =>
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
            Description = $"Консультация {specialist} после получения результатов"
        }
    ];

    private static string MapSpecialtyRu(string specialty) => specialty switch
    {
        "therapist" => "терапевт",
        "pediatrician" => "педиатр",
        "cardiologist" => "кардиолог",
        "endocrinologist" => "эндокринолог",
        "allergist" => "аллерголог",
        "neurologist" => "невролог",
        "gastroenterologist" => "гастроэнтеролог",
        "psychiatrist" => "психиатр",
        "dermatologist" => "дерматолог",
        _ => specialty
    };
}
