using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.API.Infrastructure;
using UserService.Application.DTOs.Doctor;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Application.Services;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorProfileController : ControllerBase
{
    private readonly IMultiProfileUserService _users;
    private readonly IDoctorScheduleService _schedule;

    public DoctorProfileController(IMultiProfileUserService users, IDoctorScheduleService schedule)
    {
        _users = users;
        _schedule = schedule;
    }

    /// <summary>
    /// Обновить профиль текущего врача (биография, специализация).
    /// </summary>
    [HttpPatch("me/profile")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<object>> UpdateMyProfile(
        [FromBody] UpdateDoctorProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserClaims.GetUserId(User);
            var result = await _users.UpdateDoctorProfileAsync(userId, request);
            return Ok(result);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("me/schedule/slots")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<DoctorScheduleSlotDto>> UpsertScheduleSlot(
        [FromBody] UpsertDoctorScheduleSlotRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserClaims.GetUserId(User);
            var slot = await _schedule.UpsertScheduleSlotAsync(userId, request, cancellationToken);
            return Ok(slot);
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("me/schedule/slots/{slotId:guid}")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> DeleteScheduleSlot(Guid slotId, CancellationToken cancellationToken = default)
    {
        try
        {
            var userId = UserClaims.GetUserId(User);
            await _schedule.DeleteScheduleSlotAsync(userId, slotId, cancellationToken);
            return NoContent();
        }
        catch (UserNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
