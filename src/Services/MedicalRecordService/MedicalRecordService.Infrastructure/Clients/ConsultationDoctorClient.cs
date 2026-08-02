using System.Net.Http.Json;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.Infrastructure.Clients;

public sealed class ConsultationDoctorClient : IConsultationDoctorClient
{
    private readonly HttpClient _http;
    private readonly ILogger<ConsultationDoctorClient> _logger;

    public ConsultationDoctorClient(
        HttpClient http,
        IOptions<ConsultationServiceOptions> options,
        ILogger<ConsultationDoctorClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<Guid?> GetLatestDoctorIdAsync(Guid patientId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"internal/consultations/patients/{patientId}/latest-doctor");
            request.Headers.TryAddWithoutValidation("X-Service-Name", "medical-record");

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Latest doctor lookup failed for patient {PatientId}: {Status}",
                    patientId,
                    response.StatusCode);
                return null;
            }

            var body = await response.Content.ReadFromJsonAsync<LatestDoctorResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return body?.DoctorId is { } id && id != Guid.Empty ? id : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Latest doctor lookup error for patient {PatientId}", patientId);
            return null;
        }
    }

    public async Task<bool> DoctorHasSessionWithPatientAsync(
        IReadOnlyList<Guid> doctorIdentityIds,
        IReadOnlyList<Guid> patientIdentityIds,
        CancellationToken cancellationToken = default)
    {
        if (doctorIdentityIds.Count == 0 || patientIdentityIds.Count == 0)
            return false;

        try
        {
            var doctors = string.Join(',', doctorIdentityIds.Where(id => id != Guid.Empty).Distinct());
            var patients = string.Join(',', patientIdentityIds.Where(id => id != Guid.Empty).Distinct());
            if (string.IsNullOrEmpty(doctors) || string.IsNullOrEmpty(patients))
                return false;

            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"internal/consultations/doctor-patient-link?doctorIds={Uri.EscapeDataString(doctors)}&patientIds={Uri.EscapeDataString(patients)}");
            request.Headers.TryAddWithoutValidation("X-Service-Name", "medical-record");

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Doctor-patient link check failed: {Status}",
                    response.StatusCode);
                return false;
            }

            var body = await response.Content
                .ReadFromJsonAsync<LinkResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            return body?.Linked == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Doctor-patient link check error");
            return false;
        }
    }

    private sealed class LatestDoctorResponse
    {
        public Guid DoctorId { get; set; }
    }

    private sealed class LinkResponse
    {
        public bool Linked { get; set; }
    }
}
