using System.Net.Http.Json;
using ApiGateway.Application.DTOs.Doctors;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

/// <summary>
/// Публичный каталог врачей и расписание.
/// </summary>
[ApiController]
[Route("api/v1/doctors")]
public sealed class DoctorsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;
    private readonly IDoctorCalendarService _calendar;

    public DoctorsController(IBackendForwarder backend, IDoctorCalendarService calendar)
    {
        _backend = backend;
        _calendar = calendar;
    }

    /// <summary>
    /// Поиск врачей по ФИО (query) и/или специализации.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorSearchResponseDto), StatusCodes.Status200OK)]
    public Task<IActionResult> Search(
        [FromQuery] string? specialization,
        [FromQuery] string? query,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>
        {
            $"page={Math.Max(0, page)}",
            $"pageSize={Math.Clamp(pageSize, 1, 100)}"
        };
        if (!string.IsNullOrWhiteSpace(query))
            parts.Add($"query={Uri.EscapeDataString(query)}");
        if (!string.IsNullOrWhiteSpace(specialization))
            parts.Add($"specialization={Uri.EscapeDataString(specialization)}");

        return Forward(
            _backend.ForwardAsync("user", HttpMethod.Get, $"api/doctors?{string.Join('&', parts)}", ForwardContext, cancellationToken: cancellationToken),
            cancellationToken);
    }

    [HttpGet("{doctorId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetDoctor(Guid doctorId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Get, $"api/users/{doctorId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    /// <summary>
    /// Расписание врача. doctorId — PublicId пользователя-врача (из поиска).
    /// </summary>
    [HttpGet("{doctorId:guid}/schedule")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorScheduleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetSchedule(
        Guid doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var query = new List<string>();
        if (from.HasValue)
            query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        query.Add($"days={Math.Clamp(days, 1, 30)}");
        var path = $"api/doctors/{doctorId}/schedule?{string.Join("&", query)}";
        return Forward(_backend.ForwardAsync("user", HttpMethod.Get, path, ForwardContext, cancellationToken: cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Календарь текущего врача: слоты + кем занят слот, результат триажа и анамнез пациента.
    /// </summary>
    [HttpGet("me/calendar")]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(typeof(DoctorCalendarResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DoctorCalendarResponseDto>> GetMyCalendar(
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        if (CurrentUserId is not { } doctorId)
            return Unauthorized(new { error = "Неверный идентификатор пользователя." });

        var calendar = await _calendar.GetCalendarAsync(
            doctorId,
            CurrentIdentityIds,
            from,
            days,
            ForwardContext,
            cancellationToken);

        return Ok(calendar);
    }

    [HttpPatch("me/profile")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> UpdateMyProfile(
        [FromBody] UpdateDoctorProfileRequestDto request,
        CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Patch, "api/doctors/me/profile", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("me/schedule/slots")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> UpsertScheduleSlot(
        [FromBody] UpsertDoctorScheduleSlotRequestDto request,
        CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("user", HttpMethod.Post, "api/doctors/me/schedule/slots", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpDelete("me/schedule/slots/{slotId:guid}")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> DeleteScheduleSlot(Guid slotId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("user", HttpMethod.Delete, $"api/doctors/me/schedule/slots/{slotId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
