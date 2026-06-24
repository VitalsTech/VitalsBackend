using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.Interfaces;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorScheduleController : ControllerBase
{
    private readonly IDoctorScheduleService _schedule;

    public DoctorScheduleController(IDoctorScheduleService schedule) => _schedule = schedule;

    [HttpGet("{doctorId:guid}/schedule")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSchedule(
        Guid doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default) =>
        Ok(await _schedule.GetScheduleAsync(doctorId, from, days, cancellationToken));
}
