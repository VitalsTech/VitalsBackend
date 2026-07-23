using System.Text.Json;

namespace MedicalRecordService.Domain.Enums;

public static class MedicalEventTypes
{
    public const string PatientRequestCreated = "PatientRequestCreated";
    public const string AiTriageUrgencyDetermined = "AiTriageUrgencyDetermined";
    public const string DiagnosisConfirmed = "DiagnosisConfirmed";
    public const string DiagnosisRevised = "DiagnosisRevised";
    public const string PrescriptionIssued = "PrescriptionIssued";
    public const string PrescriptionRevoked = "PrescriptionRevoked";
    public const string LabResultReceived = "LabResultReceived";
    public const string TreatmentStarted = "TreatmentStarted";
    public const string TreatmentCompleted = "TreatmentCompleted";
    public const string AllergyRecorded = "AllergyRecorded";
    public const string VitalSignRecorded = "VitalSignRecorded";
    public const string ImmunizationRecorded = "ImmunizationRecorded";
    public const string DocumentUploaded = "DocumentUploaded";

    // Informal types written by other services
    public const string ConsultationStarted = "ConsultationStarted";
    public const string ConsultationJoined = "ConsultationJoined";
    public const string ConsultationConsent = "ConsultationConsent";
    public const string ConsultationMessage = "ConsultationMessage";
    public const string ConsultationProtocolDraft = "ConsultationProtocolDraft";
    public const string ConsultationCompleted = "ConsultationCompleted";
    public const string ConsultationCancelled = "ConsultationCancelled";
    public const string ConsultationEmergency = "ConsultationEmergency";
    public const string ConsultationExpired = "ConsultationExpired";
    public const string PrescriptionSigned = "PrescriptionSigned";
    public const string PrescriptionFulfilled = "PrescriptionFulfilled";

    public static readonly IReadOnlyList<string> All =
    [
        PatientRequestCreated,
        AiTriageUrgencyDetermined,
        DiagnosisConfirmed,
        DiagnosisRevised,
        PrescriptionIssued,
        PrescriptionRevoked,
        LabResultReceived,
        TreatmentStarted,
        TreatmentCompleted,
        AllergyRecorded,
        VitalSignRecorded,
        ImmunizationRecorded,
        DocumentUploaded,
        ConsultationStarted,
        ConsultationJoined,
        ConsultationConsent,
        ConsultationMessage,
        ConsultationProtocolDraft,
        ConsultationCompleted,
        ConsultationCancelled,
        ConsultationEmergency,
        ConsultationExpired,
        PrescriptionSigned,
        PrescriptionFulfilled
    ];

    /// <summary>
    /// Maps common client aliases (e.g. "document") to canonical EventType values.
    /// </summary>
    public static IReadOnlyList<string> NormalizeFilter(IEnumerable<string> rawTypes)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in rawTypes)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var value = raw.Trim();
            if (value.Equals("document", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("documents", StringComparison.OrdinalIgnoreCase) ||
                value.Equals("attachment", StringComparison.OrdinalIgnoreCase))
            {
                result.Add(DocumentUploaded);
                continue;
            }

            var known = All.FirstOrDefault(t => t.Equals(value, StringComparison.OrdinalIgnoreCase));
            result.Add(known ?? value);
        }

        return result.ToList();
    }

    /// <summary>
    /// Maps client event aliases (e.g. "diagnosis") to canonical append EventType values.
    /// </summary>
    public static string NormalizeAppendEventType(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return raw;

        var value = raw.Trim();
        if (value.Equals("diagnosis", StringComparison.OrdinalIgnoreCase))
            return DiagnosisConfirmed;
        if (value.Equals("prescription", StringComparison.OrdinalIgnoreCase))
            return PrescriptionIssued;
        if (value.Equals("document", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("documents", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("attachment", StringComparison.OrdinalIgnoreCase))
            return DocumentUploaded;
        if (value.Equals("triage_session", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("triage", StringComparison.OrdinalIgnoreCase))
            return AiTriageUrgencyDetermined;
        if (value.Equals("mood_check", StringComparison.OrdinalIgnoreCase))
            return VitalSignRecorded;
        if (value.Equals("referral", StringComparison.OrdinalIgnoreCase))
            return TreatmentStarted;

        var known = All.FirstOrDefault(t => t.Equals(value, StringComparison.OrdinalIgnoreCase));
        return known ?? value;
    }

    /// <summary>
    /// Rewrites common client payload shapes to canonical projection fields.
    /// </summary>
    public static JsonElement NormalizeAppendPayload(string eventType, JsonElement payload)
    {
        if (payload.ValueKind != JsonValueKind.Object)
            return payload;

        using var doc = JsonDocument.Parse(payload.GetRawText());
        var root = doc.RootElement;
        var dict = new Dictionary<string, object?>();

        foreach (var property in root.EnumerateObject())
            dict[property.Name] = property.Value.Clone();

        switch (eventType)
        {
            case var _ when eventType.Equals(DiagnosisConfirmed, StringComparison.OrdinalIgnoreCase):
                MapField(dict, "icd10Code", "code", "icd10", "icd10Code");
                MapField(dict, "description", "title", "name", "description");
                break;

            case var _ when eventType.Equals(PrescriptionIssued, StringComparison.OrdinalIgnoreCase):
                MapField(dict, "medicationName", "tradeName", "medication", "medicationName", "name");
                MapField(dict, "dosage", "dosage", "scheme");
                MapField(dict, "instructions", "frequency", "instructions", "specialInstructions");
                break;

            case var _ when eventType.Equals(VitalSignRecorded, StringComparison.OrdinalIgnoreCase):
                if (!dict.ContainsKey("vitalType"))
                    dict["vitalType"] = "mood";
                if (!dict.ContainsKey("value") && dict.TryGetValue("mood", out var mood))
                    dict["value"] = mood;
                break;

            case var _ when eventType.Equals(AiTriageUrgencyDetermined, StringComparison.OrdinalIgnoreCase):
                MapField(dict, "urgencyLevel", "urgencyLevel", "urgency");
                MapField(dict, "recommendedSpecialization", "recommendedSpecialization", "specialization", "specialty");
                MapField(dict, "recommendation", "recommendation", "recommendationText", "route");
                break;
        }

        return JsonSerializer.SerializeToElement(dict);
    }

    private static void MapField(Dictionary<string, object?> dict, string target, params string[] sources)
    {
        if (dict.TryGetValue(target, out var existing) && existing is not null &&
            existing.ToString() is { Length: > 0 })
            return;

        foreach (var source in sources)
        {
            if (!dict.TryGetValue(source, out var value) || value is null)
                continue;

            var text = value switch
            {
                JsonElement element when element.ValueKind == JsonValueKind.String => element.GetString(),
                JsonElement element => element.ToString(),
                _ => value.ToString()
            };

            if (!string.IsNullOrWhiteSpace(text))
            {
                dict[target] = text;
                return;
            }
        }
    }
}
