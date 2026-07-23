using System.Net.Http.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Clients;

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
        request.Headers.TryAddWithoutValidation("X-Service-Name", "ai-triage");
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

        return new PatientMedicalContextDto
        {
            ActiveDiagnoses = state.ActiveDiagnoses.Select(d => $"{d.Icd10Code} {d.Description}").ToList(),
            ActiveMedications = state.ActivePrescriptions.Select(p => p.MedicationName).ToList(),
            Allergies = state.Allergies.Select(a => a.Allergen).ToList(),
            RecentLabHighlights = state.RecentLabResults
                .Where(l => l.IsCritical)
                .Select(l => $"{l.TestName}: {l.ResultValue}")
                .ToList()
        };
    }

    public async Task AppendTriageCompletedEventAsync(
        Guid patientId,
        Guid sessionId,
        LlmTriageResultDto result,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            sessionId,
            urgencyLevel = result.UrgencyLevel,
            recommendedSpecialization = "Терапевт",
            recommendation = result.RecommendedAction,
            canBeRemote = result.UrgencyLevel <= 3,
            route = new[]
            {
                "ИИ-триаж",
                "Запись к терапевту",
                "Консультация и назначение лечения"
            }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/medical-records/patients/{patientId}/events")
        {
            Content = JsonContent.Create(new
            {
                eventId = Guid.NewGuid(),
                eventType = "AiTriageUrgencyDetermined",
                sourceService = "ai-triage",
                payload
            })
        };
        request.Headers.TryAddWithoutValidation("X-Service-Name", "ai-triage");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());

        var response = await _http.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            _logger.LogWarning("Failed to append triage event for {PatientId}: {Status}", patientId, response.StatusCode);
    }

    public async Task<bool> DoctorHasAccessAsync(
        Guid patientId,
        IReadOnlyList<Guid> doctorIdentityIds,
        CancellationToken cancellationToken = default)
    {
        if (doctorIdentityIds.Count == 0)
            return false;

        var ids = string.Join(',', doctorIdentityIds.Distinct());
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"internal/medical-records/patients/{patientId}/doctor-access?doctorIds={Uri.EscapeDataString(ids)}");
        request.Headers.TryAddWithoutValidation("X-Service-Name", "ai-triage");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());

        try
        {
            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Doctor access check failed for patient {PatientId}: {Status}",
                    patientId,
                    response.StatusCode);
                return false;
            }

            var body = await response.Content.ReadFromJsonAsync<DoctorAccessResponse>(cancellationToken: cancellationToken);
            return body?.Allowed == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Doctor access check error for patient {PatientId}", patientId);
            return false;
        }
    }

    private sealed class DoctorAccessResponse
    {
        public bool Allowed { get; set; }
    }

    private sealed class MedicalRecordStateResponse
    {
        public List<DiagnosisItem> ActiveDiagnoses { get; set; } = new();
        public List<PrescriptionItem> ActivePrescriptions { get; set; } = new();
        public List<AllergyItem> Allergies { get; set; } = new();
        public List<LabItem> RecentLabResults { get; set; } = new();
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

    private sealed class LabItem
    {
        public string TestName { get; set; } = string.Empty;
        public string ResultValue { get; set; } = string.Empty;
        public bool IsCritical { get; set; }
    }
}
