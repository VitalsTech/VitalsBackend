using System.Net.Http.Json;
using System.Text.Json;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Clients;

public sealed class MedicalRecordEventClient : IMedicalRecordEventClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MedicalRecordEventClient> _logger;

    public MedicalRecordEventClient(HttpClient http, IOptions<MedicalRecordServiceOptions> options, ILogger<MedicalRecordEventClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task AppendConsultationEventAsync(
        Guid patientId,
        string eventType,
        object payload,
        Guid correlationId,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/medical-records/patients/{patientId}/events");
        request.Headers.TryAddWithoutValidation("X-Service-Name", "consultation-service");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());
        request.Content = JsonContent.Create(new
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.SerializeToElement(payload),
            SourceService = "consultation-service",
            CorrelationId = correlationId
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Medical record append failed for patient {PatientId}: {Status}", patientId, response.StatusCode);
    }
}
