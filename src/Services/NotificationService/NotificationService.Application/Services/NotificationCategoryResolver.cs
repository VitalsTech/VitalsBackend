namespace NotificationService.Application.Services;

internal static class NotificationCategoryResolver
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["consultation.created"] = "consultations",
        ["consultation.reminder"] = "consultations",
        ["consultation.completed"] = "consultations",
        ["message.new"] = "messages",
        ["prescription.created"] = "prescriptions",
        ["prescription.issued"] = "prescriptions",
        ["prescription.expiring_soon"] = "prescriptions",
        ["prescription.expired"] = "prescriptions",
        ["lab.result_ready"] = "labs",
        ["lab.result_critical"] = "labs",
        ["payment.failed"] = "payments",
        ["payment.completed"] = "payments",
        ["patient.mood.updated"] = "mood",
        ["patient.triage.completed"] = "triage",
        ["triage.completed"] = "triage",
        ["auto_response_required"] = "system",
        ["emergency.required"] = "system",
        ["system.maintenance"] = "system"
    };

    public static string Resolve(string eventType) =>
        Map.TryGetValue(eventType, out var category) ? category : "system";
}
