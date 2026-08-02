using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Ml;

/// <summary>Yandex Cloud AI Studio Responses API (YandexGPT).</summary>
public sealed class YandexGptLlmTriageService : ILlmTriageService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private static readonly Regex JsonFence = new(
        @"```(?:json)?\s*(.*?)```",
        RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly HttpClient _http;
    private readonly MlServicesOptions _options;
    private readonly ILogger<YandexGptLlmTriageService> _logger;

    public YandexGptLlmTriageService(
        HttpClient http,
        IOptions<MlServicesOptions> options,
        ILogger<YandexGptLlmTriageService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LlmTriageResultDto> AnalyzeAsync(TriagePromptContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
            throw new InvalidOperationException("MlServices:ApiKey не задан. Укажите ключ Yandex в appsettings.Secrets.json или переменной MlServices__ApiKey.");

        if (string.IsNullOrWhiteSpace(_options.FolderId))
            throw new InvalidOperationException("MlServices:FolderId не задан.");

        var model = string.IsNullOrWhiteSpace(_options.Model)
            ? $"gpt://{_options.FolderId}/yandexgpt-5.1/latest"
            : _options.Model;

        var endpoint = string.IsNullOrWhiteSpace(_options.ResponsesEndpoint)
            ? "https://ai.api.cloud.yandex.net/v1/responses"
            : _options.ResponsesEndpoint!;

        var requestBody = new
        {
            model,
            instructions = BuildSystemInstructions(),
            input = BuildUserInput(context),
            temperature = 0.2,
            max_output_tokens = 1200
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.TryAddWithoutValidation("Authorization", $"Api-Key {_options.ApiKey}");
        request.Headers.TryAddWithoutValidation("OpenAI-Project", _options.FolderId);
        request.Headers.TryAddWithoutValidation("x-folder-id", _options.FolderId);
        request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("YandexGPT triage failed: {Status} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"YandexGPT вернул {(int)response.StatusCode}: {Truncate(body, 400)}");
        }

        var text = ExtractOutputText(body);
        if (string.IsNullOrWhiteSpace(text))
            throw new InvalidOperationException("YandexGPT вернул пустой ответ.");

        var parsed = ParseResult(text);
        return Normalize(parsed);
    }

    private static string BuildSystemInstructions() =>
        """
        Ты — медицинский ассистент предварительного триажа платформы Vitals (Россия).
        Ты НЕ ставишь диагноз и НЕ назначаешь лекарства, процедуры или дозировки.
        Задача: уточнить жалобы, оценить срочность 1–5, предложить гипотезы (не диагноз)
        и либо задать следующий вопрос, либо предложить завершить триаж.

        Шкала срочности:
        1 — можно наблюдать дома;
        2 — плановая консультация (дни);
        3 — консультация в течение ~24 часов;
        4 — срочно (часы) / приоритетный приём;
        5 — экстренно, скорая (103), emergencyWarning=true.

        Когда ставить readyToComplete=true:
        - собраны ключевые данные: главный симптом, длительность/динамика, важные сопутствующие
          (температура, одышка, боль в груди и т.п. — по ситуации), хронические болезни/аллергии если упомянуты;
        - дополнительные вопросы уже не меняют срочность и маршрут;
        - ИЛИ emergencyWarning=true (нужно срочно завершить и направить к помощи);
        - обычно после 2–4 содержательных ответов пациента, не на первом «привет».
        Когда readyToComplete=true:
        - additionalDataNeeded = [];
        - nextQuestion можно оставить пустым или коротко резюмировать;
        - completeSuggestion — 1–2 предложения: что собрано и предложение нажать «Завершить триаж».

        Когда readyToComplete=false — задай один конкретный nextQuestion, completeSuggestion=null.

        Ответь ТОЛЬКО валидным JSON без markdown и без пояснений вне JSON:
        {
          "hypotheses": [{"condition":"строка","probability":0.0}],
          "urgencyLevel": 1,
          "nextQuestion": "вопрос пациенту на русском или пустая строка",
          "recommendedAction": "краткая рекомендация маршрута на русском",
          "additionalDataNeeded": ["что ещё уточнить"],
          "emergencyWarning": false,
          "readyToComplete": false,
          "completeSuggestion": null
        }
        probability от 0 до 1. 1–5 гипотез.
        Если данных мало — readyToComplete=false и уточняющий nextQuestion.
        """;

    private static string BuildUserInput(TriagePromptContext context)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Контекст пациента (медкарта, может быть неполным):");
        if (context.MedicalContext is null)
        {
            sb.AppendLine("- нет данных");
        }
        else
        {
            sb.AppendLine("- диагнозы: " + JoinOrDash(context.MedicalContext.ActiveDiagnoses));
            sb.AppendLine("- препараты: " + JoinOrDash(context.MedicalContext.ActiveMedications));
            sb.AppendLine("- аллергии: " + JoinOrDash(context.MedicalContext.Allergies));
            sb.AppendLine("- анализы: " + JoinOrDash(context.MedicalContext.RecentLabHighlights));
        }

        if (context.ParsedEntities.Count > 0)
            sb.AppendLine("Извлечённые сущности: " + string.Join("; ", context.ParsedEntities.Select(e => $"{e.Type}:{e.Value}")));

        sb.AppendLine();
        sb.AppendLine("Диалог:");
        foreach (var msg in context.DialogHistory.TakeLast(12))
            sb.AppendLine($"{msg.Role}: {msg.Content}");

        var patientTurns = context.DialogHistory.Count(m =>
            string.Equals(m.Role, "Patient", StringComparison.OrdinalIgnoreCase));
        sb.AppendLine();
        sb.AppendLine($"Число сообщений пациента в диалоге: {patientTurns} (текущее ещё не в списке выше, если только что отправлено).");
        sb.AppendLine("Текущее сообщение пациента:");
        sb.AppendLine(string.IsNullOrWhiteSpace(context.CurrentMessage) ? "(пациент завершил триаж без нового сообщения)" : context.CurrentMessage);
        return sb.ToString();
    }

    private static string JoinOrDash(IReadOnlyList<string> items) =>
        items.Count == 0 ? "—" : string.Join(", ", items.Take(8));

    private static string ExtractOutputText(string responseBody)
    {
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var outputText) && outputText.ValueKind == JsonValueKind.String)
            return outputText.GetString() ?? string.Empty;

        if (!root.TryGetProperty("output", out var output) || output.ValueKind != JsonValueKind.Array)
            return string.Empty;

        var sb = new StringBuilder();
        foreach (var item in output.EnumerateArray())
        {
            if (!item.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                continue;

            foreach (var part in content.EnumerateArray())
            {
                if (part.TryGetProperty("text", out var text) && text.ValueKind == JsonValueKind.String)
                    sb.Append(text.GetString());
            }
        }

        return sb.ToString().Trim();
    }

    private LlmTriageResultDto ParseResult(string text)
    {
        var json = text.Trim();
        var fence = JsonFence.Match(json);
        if (fence.Success)
            json = fence.Groups[1].Value.Trim();

        var start = json.IndexOf('{');
        var end = json.LastIndexOf('}');
        if (start >= 0 && end > start)
            json = json[start..(end + 1)];

        try
        {
            return JsonSerializer.Deserialize<LlmTriageResultDto>(json, JsonOptions)
                   ?? throw new InvalidOperationException("Пустой JSON от модели.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse YandexGPT JSON: {Text}", Truncate(text, 500));
            throw new InvalidOperationException("Не удалось разобрать ответ YandexGPT как JSON триажа.", ex);
        }
    }

    private static LlmTriageResultDto Normalize(LlmTriageResultDto result)
    {
        var urgency = Math.Clamp(result.UrgencyLevel <= 0 ? 2 : result.UrgencyLevel, 1, 5);
        var emergency = result.EmergencyWarning || urgency >= 5;
        if (emergency)
            urgency = 5;

        var additional = result.AdditionalDataNeeded?
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Take(8)
            .ToList()
            ?? [];

        var ready = result.ReadyToComplete || emergency;
        if (ready)
            additional = [];

        var nextQuestion = result.NextQuestion?.Trim() ?? string.Empty;
        if (!ready && string.IsNullOrWhiteSpace(nextQuestion))
            nextQuestion = "Опишите, пожалуйста, главный симптом подробнее.";

        var completeSuggestion = result.CompleteSuggestion?.Trim();
        if (ready && string.IsNullOrWhiteSpace(completeSuggestion))
        {
            completeSuggestion = emergency
                ? "Данных достаточно для срочного маршрута. Нажмите «Завершить триаж» — мы сразу направим вас дальше. При угрозе жизни вызывайте 103."
                : "Похоже, ключевых деталей достаточно. Можете нажать «Завершить триаж», чтобы получить маршрут к врачу.";
        }

        return new LlmTriageResultDto
        {
            Hypotheses = result.Hypotheses?
                .Where(h => !string.IsNullOrWhiteSpace(h.Condition))
                .Select(h => new HypothesisDto
                {
                    Condition = h.Condition.Trim(),
                    Probability = Math.Clamp(h.Probability, 0, 1)
                })
                .Take(5)
                .ToList()
                ?? [],
            UrgencyLevel = urgency,
            NextQuestion = nextQuestion,
            RecommendedAction = string.IsNullOrWhiteSpace(result.RecommendedAction)
                ? "Консультация врача; при ухудшении — очная помощь."
                : result.RecommendedAction.Trim(),
            AdditionalDataNeeded = additional,
            EmergencyWarning = emergency,
            ReadyToComplete = ready,
            CompleteSuggestion = ready ? completeSuggestion : null
        };
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
