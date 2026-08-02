using System.Net.Http.Json;
using System.Text.Json;
using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RoutingService.Infrastructure.Clients;

public sealed class ConsultationDispatchClient : IConsultationDispatchClient
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly HttpClient _http;
    private readonly ILogger<ConsultationDispatchClient> _logger;

    public ConsultationDispatchClient(
        HttpClient http,
        IOptions<ConsultationServiceOptions> options,
        ILogger<ConsultationDispatchClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<Guid?> CreateFromRoutingDecisionAsync(
        Guid decisionId,
        TriageCompletedEventDto triageEvent,
        RoutingEngineResult result,
        DoctorSlotDto doctor,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "internal/consultations/routing-decision");
            request.Content = JsonContent.Create(new
            {
                patientId = triageEvent.PatientId,
                sessionId = triageEvent.SessionId,
                routingDecisionId = decisionId,
                outcomeType = result.OutcomeType,
                specialist = result.Specialist,
                consultationFormat = result.ConsultationFormat,
                doctorId = doctor.DoctorId,
                doctorName = doctor.FullName,
                priority = result.Priority,
                effectiveUrgencyLevel = result.EffectiveUrgencyLevel,
                patientMessage = result.PatientMessage
            });

            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Consultation create from routing failed decision {DecisionId}: {Status} {Body}",
                    decisionId,
                    response.StatusCode,
                    body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (doc.RootElement.TryGetProperty("sessionId", out var sid) && sid.TryGetGuid(out var sessionId))
                return sessionId;
            if (doc.RootElement.TryGetProperty("SessionId", out var sid2) && sid2.TryGetGuid(out var sessionId2))
                return sessionId2;

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consultation HTTP dispatch failed for decision {DecisionId}", decisionId);
            return null;
        }
    }
}
