using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using NotificationService.Application.Options;
using Vitals.Messaging;

namespace NotificationService.Infrastructure.Messaging;

public sealed class KafkaNotificationConsumerHostedService : KafkaConsumerHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly KafkaOptions _kafka;

    public KafkaNotificationConsumerHostedService(
        ILogger<KafkaNotificationConsumerHostedService> logger,
        IOptions<KafkaConnectionOptions> kafkaConnection,
        IOptions<KafkaOptions> kafka,
        IServiceScopeFactory scopeFactory)
        : base(logger, kafkaConnection)
    {
        _scopeFactory = scopeFactory;
        _kafka = kafka.Value;
    }

    protected override IReadOnlyList<string> Topics => _kafka.Topics.ToList();

    protected override async Task HandleMessageAsync(
        string topic,
        string? key,
        string payload,
        CancellationToken cancellationToken)
    {
        var events = MapToNotificationEvents(topic, payload);
        if (events.Count == 0)
            return;

        using var scope = _scopeFactory.CreateScope();
        var orchestrator = scope.ServiceProvider.GetRequiredService<INotificationOrchestrator>();

        foreach (var notificationEvent in events)
        {
            await orchestrator.ProcessEventAsync(notificationEvent, cancellationToken).ConfigureAwait(false);
        }
    }

    internal static List<NotificationEventDto> MapToNotificationEvents(string topic, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        var root = doc.RootElement;

        return topic switch
        {
            "consultation.created" => MapConsultationCreated(root),
            "message.new" => [CreateEvent("message.new", GetGuid(root, "RecipientId", "recipientId", "PatientId", "patientId"), "Medium", new Dictionary<string, string>
            {
                ["preview"] = GetString(root, "Preview", "preview") ?? string.Empty,
                ["sender_name"] = GetString(root, "SenderName", "senderName") ?? string.Empty,
                ["sender_role"] = GetString(root, "SenderRole", "senderRole") ?? string.Empty
            })],
            "auto_response_required" => [CreateEvent("auto_response_required", GetGuid(root, "PatientId", "patientId"), "Low", new Dictionary<string, string>
            {
                ["question"] = GetString(root, "Question", "question") ?? string.Empty
            })],
            "prescription.issued" or "prescription.created" => [CreateEvent("prescription.issued", GetGuid(root, "PatientId", "patientId"), "Medium", new Dictionary<string, string>())],
            "prescription.expiring_soon" => [CreateEvent("prescription.expiring_soon", GetGuid(root, "PatientId", "patientId"), "Low", new Dictionary<string, string>())],
            "patient.mood.updated" => MapDoctorAlert(root, "patient.mood.updated"),
            "patient.triage.completed" => MapDoctorAlert(root, "patient.triage.completed"),
            "triage.completed" => MapLegacyTriageCompleted(root),
            _ => [CreateEvent(topic, GetGuid(root, "PatientId", "patientId", "UserId", "userId"), "Medium", new Dictionary<string, string>())]
        };
    }

    private static List<NotificationEventDto> MapDoctorAlert(JsonElement root, string eventType)
    {
        var patientId = GetGuid(root, "PatientId", "patientId");
        var medicalEventId = TryGetGuid(root, "MedicalEventId", "medicalEventId") ?? Guid.NewGuid();
        var priority = GetString(root, "Priority", "priority") ?? "Medium";
        var templateData = ReadTemplateData(root);
        templateData["patient_id"] = patientId.ToString();
        templateData["medical_event_id"] = medicalEventId.ToString();

        var doctors = ReadGuidArray(root, "RecipientDoctorIds", "recipientDoctorIds");
        if (doctors.Count == 0)
            return [];

        return doctors.Select(doctorId =>
        {
            var data = new Dictionary<string, string>(templateData, StringComparer.OrdinalIgnoreCase);
            return CreateEvent(
                eventType,
                doctorId,
                priority,
                data,
                eventId: DeterministicEventId(medicalEventId, doctorId, eventType));
        }).ToList();
    }

    private static List<NotificationEventDto> MapLegacyTriageCompleted(JsonElement root)
    {
        // Older publishers only had PatientId — doctor alerts go via patient.triage.completed.
        return [];
    }

    private static List<NotificationEventDto> MapConsultationCreated(JsonElement root)
    {
        var patientId = GetGuid(root, "PatientId", "patientId");
        var doctorId = GetGuid(root, "DoctorId", "doctorId");
        var doctorName = GetString(root, "DoctorName", "doctorName") ?? "врач";

        return
        [
            CreateEvent("consultation.created", patientId, "High", new Dictionary<string, string>
            {
                ["recipient_role"] = "patient",
                ["doctor_name"] = doctorName
            }),
            CreateEvent("consultation.created", doctorId, "High", new Dictionary<string, string>
            {
                ["recipient_role"] = "doctor"
            })
        ];
    }

    private static NotificationEventDto CreateEvent(
        string eventType,
        Guid userId,
        string priority,
        Dictionary<string, string> templateData,
        Guid? secondaryUserId = null,
        Guid? eventId = null) => new()
    {
        EventId = eventId ?? Guid.NewGuid(),
        EventType = eventType,
        UserId = userId,
        SecondaryUserId = secondaryUserId,
        Priority = priority,
        TemplateData = templateData
    };

    private static Guid DeterministicEventId(Guid medicalEventId, Guid doctorId, string eventType)
    {
        var input = $"{medicalEventId:N}:{doctorId:N}:{eventType}";
        var hash = MD5.HashData(Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }

    private static Dictionary<string, string> ReadTemplateData(JsonElement root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!root.TryGetProperty("TemplateData", out var data) && !root.TryGetProperty("templateData", out data))
            return result;

        if (data.ValueKind != JsonValueKind.Object)
            return result;

        foreach (var prop in data.EnumerateObject())
            result[prop.Name] = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString() ?? string.Empty
                : prop.Value.ToString();

        return result;
    }

    private static List<Guid> ReadGuidArray(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var arr) || arr.ValueKind != JsonValueKind.Array)
                continue;

            var list = new List<Guid>();
            foreach (var item in arr.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && Guid.TryParse(item.GetString(), out var g))
                    list.Add(g);
                else if (item.TryGetGuid(out g))
                    list.Add(g);
            }

            return list;
        }

        return [];
    }

    private static Guid GetGuid(JsonElement root, params string[] names)
    {
        var found = TryGetGuid(root, names);
        if (found.HasValue)
            return found.Value;

        throw new InvalidOperationException($"Payload is missing required identifier: {string.Join('/', names)}");
    }

    private static Guid? TryGetGuid(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                Guid.TryParse(value.GetString(), out var guid))
            {
                return guid;
            }

            if (root.TryGetProperty(name, out value) && value.TryGetGuid(out guid))
                return guid;
        }

        return null;
    }

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }
}
