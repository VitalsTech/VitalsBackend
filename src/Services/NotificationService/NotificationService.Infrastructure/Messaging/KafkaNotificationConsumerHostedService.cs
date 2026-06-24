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
            "auto_response_required" => [CreateEvent("auto_response_required", GetGuid(root, "PatientId"), "Low", new Dictionary<string, string>
            {
                ["question"] = GetString(root, "Question") ?? string.Empty
            })],
            "prescription.issued" or "prescription.created" => [CreateEvent("prescription.issued", GetGuid(root, "PatientId"), "Medium", new Dictionary<string, string>())],
            "prescription.expiring_soon" => [CreateEvent("prescription.expiring_soon", GetGuid(root, "PatientId"), "Low", new Dictionary<string, string>())],
            _ => [CreateEvent(topic, GetGuid(root, "PatientId", "UserId"), "Medium", new Dictionary<string, string>())]
        };
    }

    private static List<NotificationEventDto> MapConsultationCreated(JsonElement root)
    {
        var patientId = GetGuid(root, "PatientId");
        var doctorId = GetGuid(root, "DoctorId");
        var doctorName = GetString(root, "DoctorName") ?? "врач";

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
            }, patientId)
        ];
    }

    private static NotificationEventDto CreateEvent(
        string eventType,
        Guid userId,
        string priority,
        Dictionary<string, string> templateData,
        Guid? secondaryUserId = null) => new()
    {
        EventId = Guid.NewGuid(),
        EventType = eventType,
        UserId = userId,
        SecondaryUserId = secondaryUserId,
        Priority = priority,
        TemplateData = templateData
    };

    private static Guid GetGuid(JsonElement root, params string[] names)
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

        throw new InvalidOperationException($"Payload is missing required identifier: {string.Join('/', names)}");
    }

    private static string? GetString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) ? value.GetString() : null;
}
