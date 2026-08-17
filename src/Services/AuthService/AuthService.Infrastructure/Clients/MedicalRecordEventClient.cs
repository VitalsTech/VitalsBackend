using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Clients;

public sealed class MedicalRecordEventClient : IMedicalRecordEventClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MedicalRecordEventClient> _logger;

    public MedicalRecordEventClient(
        HttpClient http,
        IOptions<MedicalRecordServiceOptions> options,
        ILogger<MedicalRecordEventClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task ImportEsiaSnapshotAsync(Guid patientId, EsiaUserInfo esiaUser, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            source = "esia-test",
            importedAt = DateTime.UtcNow,
            subjectId = esiaUser.SubjectId,
            fullName = new
            {
                firstName = esiaUser.FirstName,
                middleName = esiaUser.MiddleName,
                lastName = esiaUser.LastName
            },
            birthDate = esiaUser.BirthDate,
            snils = esiaUser.Snils,
            oms = new { number = esiaUser.OmsNumber, series = esiaUser.OmsSeries },
            residenceAddress = esiaUser.ResidenceAddress,
            registrationAddress = esiaUser.RegistrationAddress,
            documents = esiaUser.MedicalDocuments,
            skipped = new[] { "clinics", "doctors" }
        };

        await AppendAsync(patientId, "EsiaImported", payload, cancellationToken);

        if (!string.IsNullOrWhiteSpace(esiaUser.OmsNumber))
        {
            await AppendAsync(patientId, "DocumentUploaded", new
            {
                documentType = "OMS",
                number = esiaUser.OmsNumber,
                series = esiaUser.OmsSeries,
                source = "esia-test"
            }, cancellationToken);
        }
    }

    private async Task AppendAsync(Guid patientId, string eventType, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, $"internal/medical-records/patients/{patientId}/events");
        request.Headers.TryAddWithoutValidation("X-Service-Name", "auth-service");
        request.Headers.TryAddWithoutValidation("X-User-Id", Guid.Empty.ToString());
        request.Content = JsonContent.Create(new
        {
            EventId = Guid.NewGuid(),
            EventType = eventType,
            Payload = JsonSerializer.SerializeToElement(payload),
            SourceService = "auth-service",
            CorrelationId = Guid.NewGuid()
        });

        var response = await _http.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning(
            "ESIA medical-record import failed for {PatientId} {EventType}: {Status} {Body}",
            patientId,
            eventType,
            response.StatusCode,
            body);
    }
}
