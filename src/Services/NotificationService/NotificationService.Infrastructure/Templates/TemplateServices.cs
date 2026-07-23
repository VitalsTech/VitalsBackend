using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Domain.Entities;

namespace NotificationService.Infrastructure.Templates;

public sealed class TemplateRenderer : ITemplateRenderer
{
    public RenderedMessage Render(NotificationTemplate template, IReadOnlyDictionary<string, string> data)
    {
        var subject = template.Subject is null ? null : ReplaceVariables(template.Subject, data);
        var body = ReplaceVariables(template.Body, data);
        return new RenderedMessage
        {
            Subject = subject,
            Body = body,
            TemplateVersion = template.Version
        };
    }

    private static string ReplaceVariables(string input, IReadOnlyDictionary<string, string> data)
    {
        var result = input;
        foreach (var pair in data)
            result = result.Replace($"{{{pair.Key}}}", pair.Value, StringComparison.OrdinalIgnoreCase);

        return result;
    }
}

public sealed class EventChannelRouter : IEventChannelRouter
{
    private static readonly Dictionary<string, (string Category, List<(string Key, string Channel)> Routes)> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        ["consultation.created"] = ("consultations", [
            ("consultation.created.patient.push", "Push"),
            ("consultation.created.patient.sms", "Sms"),
            ("consultation.created.doctor.push", "Push"),
            ("consultation.created.doctor.email", "Email")
        ]),
        ["consultation.reminder"] = ("consultations", [
            ("consultation.reminder.push", "Push"),
            ("consultation.reminder.sms", "Sms"),
            ("consultation.reminder.email", "Email")
        ]),
        ["message.new"] = ("messages", [
            ("message.new.push", "Push"),
            ("message.new.email", "Email")
        ]),
        ["prescription.created"] = ("prescriptions", [
            ("prescription.issued.push", "Push"),
            ("prescription.issued.email", "Email")
        ]),
        ["prescription.issued"] = ("prescriptions", [
            ("prescription.issued.push", "Push"),
            ("prescription.issued.email", "Email")
        ]),
        ["prescription.expiring_soon"] = ("prescriptions", [
            ("prescription.expiring.push", "Push"),
            ("prescription.expiring.email", "Email")
        ]),
        ["auto_response_required"] = ("system", [
            ("auto_response.push", "Push")
        ]),
        ["lab.result_ready"] = ("labs", [
            ("lab.result_ready.push", "Push"),
            ("lab.result_ready.sms", "Sms")
        ]),
        ["lab.result_critical"] = ("labs", [
            ("lab.result_critical.push", "Push"),
            ("lab.result_critical.sms", "Sms"),
            ("lab.result_critical.voice", "Voice")
        ]),
        ["payment.failed"] = ("payments", [
            ("payment.failed.push", "Push"),
            ("payment.failed.sms", "Sms"),
            ("payment.failed.email", "Email")
        ]),
        ["payment.completed"] = ("payments", [
            ("payment.completed.push", "Push"),
            ("payment.completed.email", "Email")
        ]),
        ["consultation.completed"] = ("consultations", [
            ("consultation.completed.patient.push", "Push"),
            ("consultation.completed.patient.email", "Email"),
            ("consultation.completed.doctor.push", "Push")
        ]),
        ["prescription.expired"] = ("prescriptions", [
            ("prescription.expired.push", "Push"),
            ("prescription.expired.email", "Email")
        ]),
        ["emergency.required"] = ("system", [
            ("emergency.required.push", "Push"),
            ("emergency.required.sms", "Sms"),
            ("emergency.required.voice", "Voice")
        ]),
        ["triage.completed"] = ("triage", [
            ("triage.completed.push", "Push")
        ]),
        ["patient.mood.updated"] = ("mood", [
            ("patient.mood.updated.push", "Push"),
            ("patient.mood.updated.email", "Email")
        ]),
        ["patient.triage.completed"] = ("triage", [
            ("patient.triage.completed.push", "Push"),
            ("patient.triage.completed.email", "Email")
        ]),
        ["system.maintenance"] = ("system", [
            ("system.maintenance.push", "Push"),
            ("system.maintenance.email", "Email")
        ])
    };

    public IReadOnlyList<(string TemplateKey, string Channel)> ResolveRoutes(NotificationEventDto notificationEvent)
    {
        if (!Map.TryGetValue(notificationEvent.EventType, out var entry))
        {
            return [("generic.system.push", "Push")];
        }

        notificationEvent.Category = entry.Category;

        if (notificationEvent.EventType.Equals("consultation.created", StringComparison.OrdinalIgnoreCase)
            && notificationEvent.TemplateData.TryGetValue("recipient_role", out var role))
        {
            return entry.Routes
                .Where(r => r.Key.Contains(role, StringComparison.OrdinalIgnoreCase))
                .Select(r => (r.Key, r.Channel))
                .ToList();
        }

        return entry.Routes.Select(r => (r.Key, r.Channel)).ToList();
    }
}
