using System.Net.Http.Json;
using System.Text.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Clients;

public sealed class RoutingDispatchClient : IRoutingDispatchClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly ILogger<RoutingDispatchClient> _logger;

    public RoutingDispatchClient(
        HttpClient http,
        IOptions<RoutingServiceOptions> options,
        ILogger<RoutingDispatchClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<RoutingDecisionSummaryDto?> DispatchTriageCompletedAsync(
        Guid sessionId,
        Guid patientId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "internal/routing/triage-completed");
            request.Content = JsonContent.Create(new
            {
                sessionId,
                patientId,
                urgencyLevel = result.UrgencyLevel,
                emergencyWarning = result.EmergencyWarning,
                patientMessageSummary = result.RecommendedAction,
                hypotheses = (result.Hypotheses ?? Array.Empty<HypothesisDto>())
                    .Select(h => new { condition = h.Condition, probability = h.Probability })
                    .ToList(),
                extractedSymptoms = result.AdditionalDataNeeded ?? Array.Empty<string>(),
                recommendedAction = result.RecommendedAction
            });

            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Routing triage-completed failed for session {SessionId}: {Status} {Body}",
                    sessionId,
                    response.StatusCode,
                    body);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var decision = await JsonSerializer.DeserializeAsync<RoutingDecisionSummaryDto>(stream, JsonOptions, cancellationToken);
            return decision;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Routing triage-completed HTTP failed for session {SessionId}", sessionId);
            return null;
        }
    }
}
