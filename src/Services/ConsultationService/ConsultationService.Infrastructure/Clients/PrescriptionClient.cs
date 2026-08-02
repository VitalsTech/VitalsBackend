using System.Net.Http.Json;
using ConsultationService.Application.Interfaces;
using ConsultationService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConsultationService.Infrastructure.Clients;

public sealed class PrescriptionClient : IPrescriptionClient
{
    private readonly HttpClient _http;
    private readonly ILogger<PrescriptionClient> _logger;

    public PrescriptionClient(HttpClient http, IOptions<PrescriptionServiceOptions> options, ILogger<PrescriptionClient> logger)
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
        string? diagnosis,
        IReadOnlyList<string> prescriptionLines,
        CancellationToken cancellationToken = default)
    {
        var medications = prescriptionLines
            .Select(ParseLine)
            .Where(m => m is not null)
            .Select(m => m!)
            .ToList();

        if (medications.Count == 0)
            return;

        using var request = new HttpRequestMessage(HttpMethod.Post, "internal/prescriptions");
        request.Content = JsonContent.Create(new
        {
            doctorId,
            patientId,
            consultationId,
            diagnosisForPrescription = string.IsNullOrWhiteSpace(diagnosis) ? "По протоколу консультации" : diagnosis.Trim(),
            confirmWarnings = true,
            medications
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning(
                "Prescription create failed for consultation {ConsultationId}: {Status} {Body}",
                consultationId,
                response.StatusCode,
                body);
        }
    }

    private static object? ParseLine(string? raw)
    {
        var line = raw?.Trim();
        if (string.IsNullOrWhiteSpace(line))
            return null;

        var parts = line.Split(new[] { '—', '-', ',' }, 2, StringSplitOptions.TrimEntries);
        var tradeName = parts[0];
        var instructions = parts.Length > 1 ? parts[1] : "По протоколу консультации";

        return new
        {
            tradeName,
            inn = tradeName,
            dosageForm = "не указана",
            dosage = instructions,
            packageQuantity = "1",
            route = "перорально",
            frequency = instructions,
            courseDays = 7,
            specialInstructions = instructions,
            atcCode = "UNSPEC",
            requiresPrescription = true
        };
    }
}
