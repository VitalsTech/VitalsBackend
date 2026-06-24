using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Domain.Enums;

namespace PrescriptionService.Infrastructure.Validation;

public sealed class RuleBasedPrescriptionValidationEngine : IPrescriptionValidationEngine
{
    private static readonly Dictionary<string, string[]> AllergyBlocks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["penicillin"] = ["амоксициллин", "amoxicillin", "penicillin", "пенициллин"],
        ["aspirin"] = ["аспирин", "aspirin", "ацетилсалициловая"]
    };

    private static readonly (string DrugA, string DrugB, string Message)[] Interactions =
    [
        ("warfarin", "aspirin", "Взаимодействие с варфарином повышает риск кровотечения. Требуется контроль МНО."),
        ("варfarin", "аспирин", "Взаимодействие с варфарином повышает риск кровотечения. Требуется контроль МНО.")
    ];

    private static readonly Dictionary<string, string> PregnancyCategoryX = new(StringComparer.OrdinalIgnoreCase)
    {
        ["isotretinoin"] = "X",
        ["изотретиноин"] = "X"
    };

    public PrescriptionValidationResultDto Validate(
        CreatePrescriptionRequest request,
        PatientMedicalContextDto? context,
        bool doctorConfirmedWarnings)
    {
        var issues = new List<ValidationIssueDto>();

        foreach (var med in request.Medications)
        {
            CheckAllergies(med, context, issues);
            CheckInteractions(med, context, issues);
            CheckDuplicates(med, context, request.Medications, issues);
            CheckDosage(med, issues);
            CheckPregnancy(med, context, issues);
        }

        if (issues.Any(i => i.Severity == "block"))
        {
            return new PrescriptionValidationResultDto
            {
                Outcome = ValidationOutcome.Blocked.ToString(),
                Issues = issues
            };
        }

        if (issues.Count > 0 && !doctorConfirmedWarnings)
        {
            return new PrescriptionValidationResultDto
            {
                Outcome = ValidationOutcome.RequiresConfirmation.ToString(),
                Issues = issues
            };
        }

        return new PrescriptionValidationResultDto
        {
            Outcome = ValidationOutcome.Allowed.ToString(),
            Issues = issues
        };
    }

    private static void CheckAllergies(MedicationItemDto med, PatientMedicalContextDto? context, List<ValidationIssueDto> issues)
    {
        if (context is null)
            return;

        foreach (var allergy in context.Allergies)
        {
            foreach (var pair in AllergyBlocks)
            {
                if (!allergy.Contains(pair.Key, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (pair.Value.Any(v => med.Inn.Contains(v, StringComparison.OrdinalIgnoreCase)
                                        || med.TradeName.Contains(v, StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Code = "ALLERGY",
                        Severity = "block",
                        Message = $"У пациента аллергия на {allergy}. Назначение {med.TradeName} невозможно."
                    });
                }
            }
        }
    }

    private static void CheckInteractions(MedicationItemDto med, PatientMedicalContextDto? context, List<ValidationIssueDto> issues)
    {
        if (context is null)
            return;

        var medName = $"{med.Inn} {med.TradeName}".ToLowerInvariant();
        foreach (var active in context.ActiveMedications)
        {
            var activeLower = active.ToLowerInvariant();
            foreach (var (drugA, drugB, message) in Interactions)
            {
                if ((medName.Contains(drugA) && activeLower.Contains(drugB))
                    || (medName.Contains(drugB) && activeLower.Contains(drugA)))
                {
                    issues.Add(new ValidationIssueDto
                    {
                        Code = "INTERACTION",
                        Severity = "warn",
                        Message = message
                    });
                }
            }
        }
    }

    private static void CheckDuplicates(
        MedicationItemDto med,
        PatientMedicalContextDto? context,
        IReadOnlyList<MedicationItemDto> batch,
        List<ValidationIssueDto> issues)
    {
        if (batch.Count(m => m.Inn.Equals(med.Inn, StringComparison.OrdinalIgnoreCase)) > 1)
        {
            issues.Add(new ValidationIssueDto
            {
                Code = "DUPLICATE_BATCH",
                Severity = "warn",
                Message = $"Препарат {med.Inn} дублируется в текущем рецепте."
            });
        }

        if (context is null)
            return;

        foreach (var active in context.ActiveMedications)
        {
            if (active.Contains(med.Inn, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssueDto
                {
                    Code = "DUPLICATE_ACTIVE",
                    Severity = "warn",
                    Message = $"Пациент уже принимает препарат с действующим веществом {med.Inn}."
                });
            }
        }
    }

    private static void CheckDosage(MedicationItemDto med, List<ValidationIssueDto> issues)
    {
        if (med.Inn.Contains("amoxicillin", StringComparison.OrdinalIgnoreCase)
            || med.Inn.Contains("амоксициллин", StringComparison.OrdinalIgnoreCase))
        {
            if (med.Frequency.Contains("4", StringComparison.Ordinal) && med.Dosage.Contains("1000"))
            {
                issues.Add(new ValidationIssueDto
                {
                    Code = "DOSAGE",
                    Severity = "warn",
                    Message = "Суточная доза амоксициллина может превышать рекомендованную. Проверьте дозировку."
                });
            }
        }
    }

    private static void CheckPregnancy(MedicationItemDto med, PatientMedicalContextDto? context, List<ValidationIssueDto> issues)
    {
        if (context?.IsPregnant != true)
            return;

        foreach (var entry in PregnancyCategoryX)
        {
            if (med.Inn.Contains(entry.Key, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new ValidationIssueDto
                {
                    Code = "PREGNANCY_X",
                    Severity = "block",
                    Message = $"Препарат {med.TradeName} категории FDA X — противопоказан при беременности."
                });
            }
        }
    }
}
