using NotificationService.API.Infrastructure;
using NotificationService.Application.DTOs;
using NotificationService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace NotificationService.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications) => _notifications = notifications;

    [HttpPost("push-tokens")]
    public async Task<IActionResult> RegisterPushToken(
        [FromBody] RegisterPushTokenRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        await _notifications.RegisterPushTokenAsync(userId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("preferences")]
    public async Task<ActionResult<IReadOnlyList<UserPreferenceDto>>> GetPreferences(CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _notifications.GetPreferencesAsync(userId, cancellationToken));
    }

    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreference(
        [FromBody] UserPreferenceDto preference,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        await _notifications.UpdatePreferenceAsync(userId, preference, cancellationToken);
        return NoContent();
    }

    [HttpGet("history")]
    public async Task<ActionResult<IReadOnlyList<NotificationDeliveryResponse>>> History(
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var userIds = UserClaims.GetIdentityIds(User);
        return Ok(await _notifications.GetUserHistoryAsync(userIds, limit, cancellationToken));
    }
}
