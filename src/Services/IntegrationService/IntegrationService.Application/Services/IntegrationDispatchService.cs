using IntegrationService.Application.DTOs;
using IntegrationService.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Vitals.ObjectStorage;

namespace IntegrationService.Application.Services;

public sealed class IntegrationDispatchService : IIntegrationDispatchService
{
    private readonly IObjectStorageProvider _storage;
    private readonly ILogger<IntegrationDispatchService> _logger;

    public IntegrationDispatchService(IObjectStorageProvider storage, ILogger<IntegrationDispatchService> logger)
    {
        _storage = storage;
        _logger = logger;
    }

    public Task<IntegrationDispatchResponse> SendSmsAsync(DispatchSmsRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("SMS provider stub -> {Phone}: {Body}", request.Phone, request.Body);
        return Task.FromResult(Accepted("sms"));
    }

    public Task<IntegrationDispatchResponse> SendEmailAsync(DispatchEmailRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Email provider stub -> {Email}: {Subject}", request.Email, request.Subject);
        return Task.FromResult(Accepted("email"));
    }

    public Task<IntegrationDispatchResponse> SendPushAsync(DispatchPushRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Push provider stub -> user={UserId} platform={Platform}: {Title}", request.UserId, request.Platform, request.Title);
        return Task.FromResult(Accepted("push"));
    }

    public Task<IntegrationDispatchResponse> SendPharmacyOrderAsync(PharmacyOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Pharmacy integration stub -> prescription={PrescriptionId} pharmacy={PharmacyId}",
            request.PrescriptionId,
            request.PharmacyId);
        return Task.FromResult(Accepted("pharmacy"));
    }

    public Task<IntegrationDispatchResponse> SendLabOrderAsync(LabOrderRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Lab integration stub -> patient={PatientId} labs={Labs}",
            request.PatientId,
            string.Join(", ", request.Labs));
        return Task.FromResult(Accepted("lab"));
    }

    public Task<IntegrationDispatchResponse> DispatchEmergencyAsync(EmergencyDispatchRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogCritical(
            "EMERGENCY DISPATCH stub -> patient={PatientId} session={SessionId} urgency={Urgency} symptoms={Symptoms}",
            request.PatientId,
            request.SessionId,
            request.UrgencyLevel,
            request.Symptoms);
        return Task.FromResult(Accepted("emergency"));
    }

    public Task<PaymentProcessResponse> ProcessPaymentAsync(PaymentProcessRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Payment provider stub -> payment={PaymentId} user={UserId} amount={Amount} {Currency}",
            request.PaymentId,
            request.UserId,
            request.Amount,
            request.Currency);

        if (request.Amount <= 0)
        {
            return Task.FromResult(new PaymentProcessResponse
            {
                Status = "failed",
                FailureReason = "Invalid amount"
            });
        }

        return Task.FromResult(new PaymentProcessResponse
        {
            Status = "completed",
            ExternalReference = $"pay-{Guid.NewGuid():N}"
        });
    }

    public Task<IntegrationDispatchResponse> SendVoiceCallAsync(VoiceCallRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Voice call provider stub -> user={UserId} phone={Phone} urgency={Urgency}: {Message}",
            request.UserId,
            request.Phone,
            request.UrgencyLevel,
            request.Message);
        return Task.FromResult(Accepted("voice"));
    }

    public Task<EgiszPreferentialCheckResponse> CheckPreferentialEligibilityAsync(
        EgiszPreferentialCheckRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "EGISZ preferential check stub -> patient={PatientId} category={Category}",
            request.PatientId,
            request.PreferentialCategory);

        var eligible = !string.IsNullOrWhiteSpace(request.PreferentialCategory);
        return Task.FromResult(new EgiszPreferentialCheckResponse
        {
            IsEligible = eligible,
            RejectionReason = eligible ? null : "Preferential category is required",
            EgiszReference = eligible ? $"egisz-{Guid.NewGuid():N}" : null
        });
    }

    public async Task<StorageUploadResponse> UploadObjectAsync(
        string objectKey,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var stored = await _storage.UploadAsync(new StoreObjectRequest
        {
            ObjectKey = objectKey,
            Content = content,
            ContentType = contentType
        }, cancellationToken).ConfigureAwait(false);

        return new StorageUploadResponse
        {
            ObjectKey = stored.ObjectKey,
            Url = stored.Url,
            CdnUrl = stored.CdnUrl,
            SizeBytes = stored.SizeBytes,
            ContentType = stored.ContentType
        };
    }

    private static IntegrationDispatchResponse Accepted(string channel) => new()
    {
        RequestId = Guid.NewGuid(),
        Status = "accepted",
        ExternalReference = $"{channel}-{Guid.NewGuid():N}"
    };
}
