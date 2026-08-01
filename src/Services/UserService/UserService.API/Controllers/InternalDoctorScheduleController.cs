using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Doctor;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;

namespace UserService.API.Controllers;

/// <summary>
/// Бронирование слотов и расписание с данными брони. Вызывается только шлюзом:
/// право записи проверяется там по JWT, здесь — ключ сервиса.
/// </summary>
[ApiController]
[Route("internal/doctors")]
[Authorize(Policy = "InternalService")]
public sealed class InternalDoctorScheduleController : ControllerBase
{
    private readonly IDoctorScheduleService _schedule;

    public InternalDoctorScheduleController(IDoctorScheduleService schedule) => _schedule = schedule;

    [HttpGet("{doctorId:guid}/schedule")]
    public async Task<ActionResult<DoctorScheduleBookingResponseDto>> GetSchedule(
        Guid doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await _schedule.GetScheduleWithBookingsAsync(doctorId, from, days, cancellationToken));
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{doctorId:guid}/schedule/slots/{slotId:guid}/reserve")]
    public async Task<ActionResult<DoctorScheduleSlotBookingDto>> Reserve(
        Guid doctorId,
        Guid slotId,
        [FromBody] ReserveScheduleSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.PatientId == Guid.Empty)
            return BadRequest(new { error = "patientId обязателен." });

        try
        {
            var slot = await _schedule.ReserveScheduleSlotAsync(doctorId, slotId, request.PatientId, cancellationToken);
            if (slot is null)
                return Conflict(new { error = "Слот уже занят или недоступен для записи." });

            return Ok(slot);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("schedule/slots/{slotId:guid}/release")]
    public async Task<IActionResult> Release(
        Guid slotId,
        [FromBody] ReserveScheduleSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        var released = await _schedule.ReleaseScheduleSlotAsync(slotId, request.PatientId, cancellationToken);
        return released ? NoContent() : NotFound(new { error = "Бронь не найдена." });
    }

    [HttpPost("schedule/slots/{slotId:guid}/link-session")]
    public async Task<IActionResult> LinkSession(
        Guid slotId,
        [FromBody] LinkScheduleSlotSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        var linked = await _schedule.LinkScheduleSlotSessionAsync(
            slotId,
            request.PatientId,
            request.ConsultationSessionId,
            cancellationToken);

        return linked ? NoContent() : NotFound(new { error = "Бронь не найдена." });
    }
}

public sealed class ReserveScheduleSlotRequest
{
    public Guid PatientId { get; set; }
}

public sealed class LinkScheduleSlotSessionRequest
{
    public Guid PatientId { get; set; }
    public Guid ConsultationSessionId { get; set; }
}
