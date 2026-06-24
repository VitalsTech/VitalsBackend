using System.Text.RegularExpressions;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;

namespace AITriageService.Infrastructure.Parsing;

public sealed class RuleBasedSymptomParser : ISymptomParser
{
    private static readonly (string Pattern, string Symptom)[] SymptomPatterns =
    [
        ("головн\\w*\\s*бол", "головная боль"),
        ("кашел", "кашель"),
        ("температур", "температура"),
        ("тошнот", "тошнота"),
        ("одышк", "одышка"),
        ("онемен", "онемение"),
        ("бол(?:ь|ит)\\s+в\\s+груди", "боль в груди")
    ];

    private static readonly (string Pattern, string Location)[] LocationPatterns =
    [
        ("в\\s+виск", "виски"),
        ("в\\s+груди", "грудь"),
        ("в\\s+живот", "живот"),
        ("в\\s+ног", "ноги"),
        ("в\\s+голов", "голова"),
        ("в\\s+сустав", "суставы")
    ];

    private static readonly (string Pattern, string Character)[] CharacterPatterns =
    [
        ("пульсирующ", "пульсирующая"),
        ("ноющ", "ноющая"),
        ("остр", "острая"),
        ("слаб", "слабая"),
        ("сильн", "сильная"),
        ("невыносим", "невыносимая")
    ];

    private static readonly (string Pattern, string Dynamic)[] DynamicPatterns =
    [
        ("усилива", "усиливается"),
        ("ослабева", "ослабевает"),
        ("постоянн", "постоянная")
    ];

    private static readonly (string Pattern, string Condition)[] ChronicPatterns =
    [
        ("диабет", "диабет"),
        ("гипертон", "гипертония"),
        ("астм", "астма")
    ];

    public IReadOnlyList<ExtractedEntityDto> Parse(string text)
    {
        var normalized = text.ToLowerInvariant();
        var entities = new List<ExtractedEntityDto>();

        foreach (var (pattern, symptom) in SymptomPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                entities.Add(new ExtractedEntityDto { Type = "Symptom", Value = symptom });
        }

        foreach (var (pattern, location) in LocationPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                entities.Add(new ExtractedEntityDto { Type = "Location", Value = location });
        }

        var durationMatch = Regex.Match(normalized, @"(?:уже\s+)?(?:(\d+)\s*(?:‑|-)?\s*(?:й|й|й)?\s*(день|дня|дней|недел\w*)|с\s+утра|уже\s+недел\w*)");
        if (durationMatch.Success)
        {
            var value = durationMatch.Value.Trim();
            entities.Add(new ExtractedEntityDto { Type = "Duration", Value = value });
        }

        foreach (var (pattern, character) in CharacterPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                entities.Add(new ExtractedEntityDto { Type = "Characteristic", Value = character });
        }

        foreach (var (pattern, dynamic) in DynamicPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                entities.Add(new ExtractedEntityDto { Type = "Dynamic", Value = dynamic });
        }

        if (Regex.IsMatch(normalized, "не\\s+помога", RegexOptions.IgnoreCase))
            entities.Add(new ExtractedEntityDto { Type = "Modifier", Value = "не помогает терапия" });

        if (Regex.IsMatch(normalized, "после\\s+ед", RegexOptions.IgnoreCase))
            entities.Add(new ExtractedEntityDto { Type = "Trigger", Value = "после еды" });
        if (Regex.IsMatch(normalized, "ноч", RegexOptions.IgnoreCase))
            entities.Add(new ExtractedEntityDto { Type = "Trigger", Value = "ночью" });
        if (Regex.IsMatch(normalized, "нагруз", RegexOptions.IgnoreCase))
            entities.Add(new ExtractedEntityDto { Type = "Trigger", Value = "при физической нагрузке" });

        var ageMatch = Regex.Match(normalized, @"(\d{1,3})\s*(?:лет|года|год)\b");
        if (ageMatch.Success)
            entities.Add(new ExtractedEntityDto { Type = "Age", Value = ageMatch.Groups[1].Value, Detail = "years" });

        foreach (var (pattern, condition) in ChronicPatterns)
        {
            if (Regex.IsMatch(normalized, pattern, RegexOptions.IgnoreCase))
                entities.Add(new ExtractedEntityDto { Type = "ChronicCondition", Value = condition });
        }

        return entities
            .GroupBy(e => $"{e.Type}:{e.Value}")
            .Select(g => g.First())
            .ToList();
    }
}
