using IntegrationService.Application.DTOs;

namespace IntegrationService.Application.Interfaces;

public interface IIntegrationDispatchService
{
    Task<IntegrationDispatchResponse> SendSmsAsync(DispatchSmsRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> SendEmailAsync(DispatchEmailRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> SendPushAsync(DispatchPushRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> SendPharmacyOrderAsync(PharmacyOrderRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> SendLabOrderAsync(LabOrderRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> DispatchEmergencyAsync(EmergencyDispatchRequest request, CancellationToken cancellationToken = default);
    Task<PaymentProcessResponse> ProcessPaymentAsync(PaymentProcessRequest request, CancellationToken cancellationToken = default);
    Task<IntegrationDispatchResponse> SendVoiceCallAsync(VoiceCallRequest request, CancellationToken cancellationToken = default);
    Task<EgiszPreferentialCheckResponse> CheckPreferentialEligibilityAsync(EgiszPreferentialCheckRequest request, CancellationToken cancellationToken = default);
    Task<StorageUploadResponse> UploadObjectAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken = default);
}
