using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;

namespace AITriageService.Infrastructure.Ml;

/// <summary>Stub LLM: формирует гипотезы, срочность и следующий вопрос без внешней модели.</summary>
public sealed class StubLlmTriageService : ILlmTriageService
{
    private static readonly string[] EmergencyPatterns =
    [
        "скорую", "скорой", "задыха", "боль в груди", "обморок", "кров", "сильная боль в груди", "не могу дышать"
    ];

    public Task<LlmTriageResultDto> AnalyzeAsync(TriagePromptContext context, CancellationToken cancellationToken = default)
    {
        var text = context.CurrentMessage.ToLowerInvariant();
        var hasHeadache = context.ParsedEntities.Any(e => e.Value.Contains("голов", StringComparison.OrdinalIgnoreCase));
        var hasCough = context.ParsedEntities.Any(e => e.Value.Contains("каш", StringComparison.OrdinalIgnoreCase));
        var hasDiabetes = context.MedicalContext?.ActiveDiagnoses.Any(d => d.Contains("диабет", StringComparison.OrdinalIgnoreCase)) == true
            || context.ParsedEntities.Any(e => e.Value.Contains("диабет", StringComparison.OrdinalIgnoreCase));
        var hasNumbness = context.ParsedEntities.Any(e => e.Value.Contains("онем", StringComparison.OrdinalIgnoreCase));

        var emergency = EmergencyPatterns.Any(p => text.Contains(p, StringComparison.OrdinalIgnoreCase));
        var urgency = emergency ? 5 : hasHeadache ? 2 : hasCough ? 3 : 2;

        var hypotheses = new List<HypothesisDto>();
        if (hasHeadache)
        {
            hypotheses.Add(new HypothesisDto { Condition = "Головная боль напряжения / мигрень", Probability = 0.55 });
            hypotheses.Add(new HypothesisDto { Condition = "Побочное действие терапии / обезвоживание", Probability = 0.25 });
            hypotheses.Add(new HypothesisDto { Condition = "Иное состояние, требующее очного осмотра", Probability = 0.20 });
        }
        else if (hasCough)
        {
            hypotheses.Add(new HypothesisDto { Condition = "ОРВИ", Probability = 0.70 });
            hypotheses.Add(new HypothesisDto { Condition = "Грипп", Probability = 0.20 });
            hypotheses.Add(new HypothesisDto { Condition = "Аллергический компонент", Probability = 0.10 });
        }
        else
        {
            hypotheses.Add(new HypothesisDto { Condition = "Требуется дополнительное уточнение симптомов", Probability = 0.60 });
            hypotheses.Add(new HypothesisDto { Condition = "Безопасное наблюдение дома", Probability = 0.40 });
        }

        var nextQuestion = BuildNextQuestion(hasHeadache, hasCough, hasDiabetes, hasNumbness, emergency);
        var action = emergency
            ? "Немедленно вызовите скорую помощь (103). Не полагайтесь только на онлайн-оценку."
            : urgency >= 4
                ? "Срочная очная консультация или вызов скорой."
                : urgency == 3
                    ? "Консультация врача в течение 24 часов."
                    : "Наблюдение, при ухудшении — очная консультация.";

        var additional = new List<string>();
        if (hasHeadache)
            additional.AddRange(["точная локализация боли", "наличие тошноты или нарушений зрения", "принимаемые препараты"]);
        if (hasDiabetes && hasNumbness)
            additional.Add("уровень глюкозы за последние сутки");

        var result = new LlmTriageResultDto
        {
            Hypotheses = hypotheses,
            UrgencyLevel = urgency,
            NextQuestion = nextQuestion,
            RecommendedAction = action,
            AdditionalDataNeeded = additional,
            EmergencyWarning = emergency
        };

        return Task.FromResult(result);
    }

    private static string BuildNextQuestion(bool headache, bool cough, bool diabetes, bool numbness, bool emergency)
    {
        if (emergency)
            return "Пожалуйста, вызовите скорую помощь. Вы в безопасности сейчас? Опишите, что происходит в данный момент.";

        if (diabetes && numbness)
            return "Вы упомянули онемение. Когда вы последний раз измеряли уровень сахара в крови и какое было значение?";

        if (headache)
            return "Боль усиливается при движении или изменении положения головы? Есть ли тошнота, рвота или нарушения зрения?";

        if (cough)
            return "Кашель сухой или с мокротой? Какая максимальная температура была за последние сутки?";

        return "Опишите, пожалуйста, главный симптом подробнее: когда начался, как меняется интенсивность и что облегчает или усиливает его.";
    }
}
