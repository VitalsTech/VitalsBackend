using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("internal/notifications")]
public sealed class InternalNotificationController : ControllerBase
{
    private readonly INotificationService _notifications;

    public InternalNotificationController(INotificationService notifications) => _notifications = notifications;

    [HttpPost("events")]
    public async Task<ActionResult<ProcessEventResponse>> ProcessEvent(
        [FromBody] NotificationEventDto request,
        CancellationToken cancellationToken) =>
        Ok(await _notifications.ProcessEventAsync(request, cancellationToken));

    [HttpPost("send")]
    public async Task<ActionResult<ProcessEventResponse>> SendManual(
        [FromBody] SendManualNotificationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _notifications.SendManualAsync(request, cancellationToken));

    [HttpGet("{deliveryId:guid}/status")]
    public async Task<ActionResult<NotificationDeliveryResponse>> GetStatus(Guid deliveryId, CancellationToken cancellationToken)
    {
        var status = await _notifications.GetDeliveryStatusAsync(deliveryId, cancellationToken);
        return status is null ? NotFound() : Ok(status);
    }

    [HttpGet("users/{userId:guid}/history")]
    public async Task<ActionResult<IReadOnlyList<NotificationDeliveryResponse>>> History(
        Guid userId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        Ok(await _notifications.GetUserHistoryAsync(userId, limit, cancellationToken));

    [HttpGet("stats")]
    public async Task<ActionResult<NotificationStatsResponse>> Stats(CancellationToken cancellationToken) =>
        Ok(await _notifications.GetStatsAsync(cancellationToken));

    [HttpGet("dlq")]
    public async Task<ActionResult<IReadOnlyList<DeadLetterNotificationDto>>> DeadLetters(
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default) =>
        Ok(await _notifications.GetDeadLettersAsync(limit, cancellationToken));

    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<NotificationTemplateDto>>> Templates(CancellationToken cancellationToken) =>
        Ok(await _notifications.GetTemplatesAsync(cancellationToken));

    [HttpPut("templates/{templateKey}/{channel}")]
    public async Task<ActionResult<NotificationTemplateDto>> UpdateTemplate(
        string templateKey,
        string channel,
        [FromBody] UpdateTemplateRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _notifications.UpdateTemplateAsync(templateKey, channel, request, cancellationToken));
}
