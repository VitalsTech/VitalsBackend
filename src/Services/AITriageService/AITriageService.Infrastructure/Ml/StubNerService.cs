using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;

namespace AITriageService.Infrastructure.Ml;

/// <summary>Stub NER: нормализует сущности и добавляет коды МКБ-10.</summary>
public sealed class StubNerService : INerService
{
    private static readonly Dictionary<string, (string Normalized, string Icd10, string Type)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["головная боль"] = ("Головная боль", "R51", "Symptom"),
        ["кашель"] = ("Кашель", "R05", "Symptom"),
        ["температура"] = ("Лихорадка", "R50", "Symptom"),
        ["тошнота"] = ("Тошнота", "R11", "Symptom"),
        ["одышка"] = ("Одышка", "R06.0", "Symptom"),
        ["онемение"] = ("Онемение", "R20.0", "Symptom"),
        ["боль в груди"] = ("Боль в груди", "R07.4", "Symptom"),
        ["диабет"] = ("Сахарный диабет", "E11", "Diagnosis"),
        ["гипертония"] = ("Гипертония", "I10", "Diagnosis")
    };

    public Task<IReadOnlyList<NerEntityDto>> ExtractAsync(
        string text,
        IReadOnlyList<ExtractedEntityDto> parsed,
        CancellationToken cancellationToken = default)
    {
        var results = new List<NerEntityDto>();

        foreach (var entity in parsed)
        {
            if (Map.TryGetValue(entity.Value, out var mapped))
            {
                results.Add(new NerEntityDto
                {
                    Text = entity.Value,
                    EntityType = mapped.Type,
                    NormalizedTerm = mapped.Normalized,
                    Icd10Code = mapped.Icd10
                });
            }
            else
            {
                results.Add(new NerEntityDto
                {
                    Text = entity.Value,
                    EntityType = entity.Type,
                    NormalizedTerm = entity.Value
                });
            }
        }

        return Task.FromResult<IReadOnlyList<NerEntityDto>>(results);
    }
}
