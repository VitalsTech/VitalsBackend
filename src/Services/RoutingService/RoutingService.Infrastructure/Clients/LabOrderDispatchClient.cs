using System.Net.Http.Json;
using RoutingService.Application.Interfaces;
using RoutingService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace RoutingService.Infrastructure.Clients;

public sealed class LabOrderDispatchClient : ILabOrderDispatchClient
{
    private readonly HttpClient _http;
    private readonly ILogger<LabOrderDispatchClient> _logger;

    public LabOrderDispatchClient(
        HttpClient http,
        IOptions<PrescriptionServiceOptions> options,
        ILogger<LabOrderDispatchClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task CreateFromRoutingAsync(
        Guid patientId,
        Guid? doctorId,
        Guid? consultationId,
        IReadOnlyList<string> labs,
        string priority,
        CancellationToken cancellationToken = default)
    {
        var items = labs
            .Select(l => l?.Trim())
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .Select(l => new { testName = l })
            .ToList();

        if (items.Count == 0 || doctorId is null || doctorId == Guid.Empty)
        {
            _logger.LogInformation(
                "Skip lab-order create for patient {PatientId}: items={Count} doctor={DoctorId}",
                patientId,
                items.Count,
                doctorId);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "internal/lab-orders");
            request.Content = JsonContent.Create(new
            {
                doctorId,
                patientId,
                consultationId,
                clinicalIndication = "По решению маршрутизации (анализы до консультации)",
                priority,
                items
            });

            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "Lab order create from routing failed patient {PatientId}: {Status} {Body}",
                    patientId,
                    response.StatusCode,
                    body);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Lab order HTTP dispatch failed for patient {PatientId}", patientId);
        }
    }
}
