using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Doctor;
using UserService.Application.Interfaces;

namespace UserService.API.Controllers;

[ApiController]
[Route("api/doctors")]
public sealed class DoctorsController : ControllerBase
{
    private readonly IDoctorScheduleService _doctors;

    public DoctorsController(IDoctorScheduleService doctors) => _doctors = doctors;

    /// <summary>
    /// Поиск врачей по ФИО и/или специализации.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorSearchResponseDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DoctorSearchResponseDto>> Search(
        [FromQuery] string? query,
        [FromQuery] string? specialization,
        [FromQuery] int page = 0,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await _doctors.SearchDoctorsAsync(new DoctorSearchRequest
        {
            Query = query,
            Specialization = specialization,
            Page = page,
            PageSize = pageSize
        }, cancellationToken));

    /// <summary>
    /// Расписание врача. <paramref name="doctorId"/> — User.PublicId (или ProfileId врача).
    /// </summary>
    [HttpGet("{doctorId:guid}/schedule")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(DoctorScheduleResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoctorScheduleResponseDto>> GetSchedule(
        Guid doctorId,
        [FromQuery] DateTime? from,
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default) =>
        Ok(await _doctors.GetScheduleAsync(doctorId, from, days, cancellationToken));
}
