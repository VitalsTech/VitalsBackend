using System.Net.Http.Json;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Clients;

public sealed class RoutingClient : IRoutingClient
{
    private readonly HttpClient _http;
    private readonly ILogger<RoutingClient> _logger;

    public RoutingClient(HttpClient http, IOptions<RoutingServiceOptions> options, ILogger<RoutingClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task AppendPostConsultationLabsAsync(
        Guid patientId,
        Guid doctorId,
        Guid consultationId,
        IReadOnlyList<string> labs,
        CancellationToken cancellationToken = default)
    {
        var clean = labs
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Cast<string>()
            .ToList();

        if (clean.Count == 0)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/routing/patients/{patientId}/labs");
        request.Content = JsonContent.Create(new
        {
            consultationId,
            doctorId,
            labs = clean
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Routing append labs failed for patient {PatientId} consultation {ConsultationId}: {Status} {Body}",
                patientId,
                consultationId,
                response.StatusCode,
                body);
        }
    }
}
