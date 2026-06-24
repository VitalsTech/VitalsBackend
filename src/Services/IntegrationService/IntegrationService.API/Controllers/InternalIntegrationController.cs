using IntegrationService.Application.DTOs;
using IntegrationService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IntegrationService.API.Controllers;

[ApiController]
[Route("internal/integration")]
[Authorize(Policy = "InternalService")]
public sealed class InternalIntegrationController : ControllerBase
{
    private readonly IIntegrationDispatchService _dispatch;

    public InternalIntegrationController(IIntegrationDispatchService dispatch) => _dispatch = dispatch;

    [HttpPost("sms/send")]
    public Task<IntegrationDispatchResponse> SendSms([FromBody] DispatchSmsRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendSmsAsync(request, cancellationToken);

    [HttpPost("email/send")]
    public Task<IntegrationDispatchResponse> SendEmail([FromBody] DispatchEmailRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendEmailAsync(request, cancellationToken);

    [HttpPost("push/send")]
    public Task<IntegrationDispatchResponse> SendPush([FromBody] DispatchPushRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendPushAsync(request, cancellationToken);

    [HttpPost("pharmacy/orders")]
    public Task<IntegrationDispatchResponse> SendPharmacyOrder([FromBody] PharmacyOrderRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendPharmacyOrderAsync(request, cancellationToken);

    [HttpPost("lab/orders")]
    public Task<IntegrationDispatchResponse> SendLabOrder([FromBody] LabOrderRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendLabOrderAsync(request, cancellationToken);

    [HttpPost("emergency/dispatch")]
    public Task<IntegrationDispatchResponse> DispatchEmergency([FromBody] EmergencyDispatchRequest request, CancellationToken cancellationToken) =>
        _dispatch.DispatchEmergencyAsync(request, cancellationToken);

    [HttpPost("payments/process")]
    public Task<PaymentProcessResponse> ProcessPayment([FromBody] PaymentProcessRequest request, CancellationToken cancellationToken) =>
        _dispatch.ProcessPaymentAsync(request, cancellationToken);

    [HttpPost("voice/call")]
    public Task<IntegrationDispatchResponse> SendVoice([FromBody] VoiceCallRequest request, CancellationToken cancellationToken) =>
        _dispatch.SendVoiceCallAsync(request, cancellationToken);

    [HttpPost("egisz/preferential/check")]
    public Task<EgiszPreferentialCheckResponse> CheckPreferential([FromBody] EgiszPreferentialCheckRequest request, CancellationToken cancellationToken) =>
        _dispatch.CheckPreferentialEligibilityAsync(request, cancellationToken);

    [HttpPost("storage/upload")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<StorageUploadResponse>> UploadObject(
        IFormFile file,
        [FromQuery] string objectKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
            return BadRequest("objectKey is required.");

        await using var stream = file.OpenReadStream();
        var result = await _dispatch.UploadObjectAsync(objectKey, stream, file.ContentType, cancellationToken);
        return Ok(result);
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "integration-service" });
}
