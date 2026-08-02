using System.Net.Http.Json;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Clients;

public sealed class LabOrderClient : ILabOrderClient
{
    private readonly HttpClient _http;
    private readonly ILogger<LabOrderClient> _logger;

    public LabOrderClient(HttpClient http, IOptions<PrescriptionServiceOptions> options, ILogger<LabOrderClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task CreateFromConsultationAsync(
        Guid patientId,
        Guid doctorId,
        Guid consultationId,
        IReadOnlyList<string> labNames,
        CancellationToken cancellationToken = default)
    {
        var items = labNames
            .Select(n => n?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Select(n => new { testName = n })
            .ToList();

        if (items.Count == 0)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/lab-orders");
        request.Content = JsonContent.Create(new
        {
            doctorId,
            patientId,
            consultationId,
            clinicalIndication = "По протоколу консультации",
            priority = "routine",
            items
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Lab order create failed for consultation {ConsultationId}: {Status} {Body}",
                consultationId,
                response.StatusCode,
                body);
        }
    }
}
