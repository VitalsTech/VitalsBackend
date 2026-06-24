using System.Net.Http.Json;
using System.Text.Json;
using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using PrescriptionService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace PrescriptionService.Infrastructure.Clients;

public sealed class MedicalRecordContextClient : IMedicalRecordContextClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MedicalRecordContextClient> _logger;

    public MedicalRecordContextClient(HttpClient http, IOptions<MedicalRecordServiceOptions> options, ILogger<MedicalRecordContextClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<PatientMedicalContextDto?> GetContextAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/medical-records/patients/{patientId}/state");
        request.Headers.TryAddWithoutValidation("X-Service-Name", "prescription-service");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Medical record context unavailable for {PatientId}: {Status}", patientId, response.StatusCode);
            return null;
        }

        var state = await response.Content.ReadFromJsonAsync<MedicalRecordStateResponse>(cancellationToken: cancellationToken);
        if (state is null)
            return null;

        var diagnoses = state.ActiveDiagnoses.Select(d => $"{d.Icd10Code} {d.Description}").ToList();
        return new PatientMedicalContextDto
        {
            ActiveDiagnoses = diagnoses,
            ActiveMedications = state.ActivePrescriptions.Select(p => p.MedicationName).ToList(),
            Allergies = state.Allergies.Select(a => a.Allergen).ToList(),
            IsPregnant = diagnoses.Any(d => d.Contains("O00", StringComparison.OrdinalIgnoreCase) || d.Contains("беремен", StringComparison.OrdinalIgnoreCase))
        };
    }

    private sealed class MedicalRecordStateResponse
    {
        public List<DiagnosisItem> ActiveDiagnoses { get; set; } = new();
        public List<PrescriptionItem> ActivePrescriptions { get; set; } = new();
        public List<AllergyItem> Allergies { get; set; } = new();
    }

    private sealed class DiagnosisItem
    {
        public string Icd10Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private sealed class PrescriptionItem
    {
        public string MedicationName { get; set; } = string.Empty;
    }

    private sealed class AllergyItem
    {
        public string Allergen { get; set; } = string.Empty;
    }
}

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

    public async Task AppendEventAsync(Guid patientId, string eventType, object payload, Guid correlationId, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/medical-records/patients/{patientId}/events");
        request.Headers.TryAddWithoutValidation("X-Service-Name", "prescription-service");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());
        request.Content = JsonContent.Create(new
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.SerializeToElement(payload),
            SourceService = "prescription-service",
            CorrelationId = correlationId
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Medical record append failed for {PatientId}: {Status}", patientId, response.StatusCode);
    }
}

public sealed class StubUserPermissionClient : IUserPermissionClient
{
    private static readonly HashSet<string> PsychiatricAtc = new(StringComparer.OrdinalIgnoreCase) { "N05", "N06" };

    public Task<bool> CanPrescribeAsync(Guid doctorId, string atcCode, CancellationToken cancellationToken = default)
    {
        if (PsychiatricAtc.Any(prefix => atcCode.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
            return Task.FromResult(false);

        return Task.FromResult(true);
    }
}

public sealed class StubPharmacyIntegrationClient : IPharmacyIntegrationClient
{
    public Task<PharmacyDispatchResultDto> SendPrescriptionAsync(
        Guid prescriptionId,
        Guid pharmacyId,
        Domain.Entities.Prescription prescription,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(new PharmacyDispatchResultDto
        {
            Success = true,
            OrderId = $"PH-{prescriptionId:N}".Substring(0, 20),
            Message = $"Stub booking at pharmacy {pharmacyId}"
        });
}

public sealed class StubESignatureService : IESignatureService
{
    public Task<string> SignPrescriptionAsync(Domain.Entities.Prescription prescription, CancellationToken cancellationToken = default)
    {
        var payload = $"{prescription.Id}|{prescription.PatientId}|{prescription.DoctorId}|{prescription.SignedAt?.Ticks ?? DateTime.UtcNow.Ticks}";
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"STUB-SIG:{payload}"));
        return Task.FromResult(signature);
    }
}
