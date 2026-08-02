using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationService.API.Controllers;

[ApiController]
[Route("internal/consultations")]
public sealed class InternalConsultationController : ControllerBase
{
    private readonly IConsultationService _consultations;
    private readonly IConsultationRepository _sessions;

    public InternalConsultationController(
        IConsultationService consultations,
        IConsultationRepository sessions)
    {
        _consultations = consultations;
        _sessions = sessions;
    }

    [HttpPost("routing-decision")]
    public async Task<ActionResult<ConsultationSessionResponse>> CreateFromRoutingDecision(
        [FromBody] RoutingDecisionEventDto request,
        CancellationToken cancellationToken)
    {
        if (request.OutcomeType != "Consultation" && request.OutcomeType != "LabsBeforeConsultation")
            return BadRequest(new { error = "Only consultation outcomes create sessions." });

        return Ok(await _consultations.CreateFromRoutingDecisionAsync(request, cancellationToken));
    }

    /// <summary>
    /// Latest consultation doctor for a patient (for mood/triage notification recipients).
    /// </summary>
    [HttpGet("patients/{patientId:guid}/latest-doctor")]
    public async Task<IActionResult> GetLatestDoctor(Guid patientId, CancellationToken cancellationToken)
    {
        var session = await _sessions.FindLatestByPatientAsync(patientId, cancellationToken);
        if (session is null)
            return NotFound();

        return Ok(new { doctorId = session.DoctorId, sessionId = session.Id });
    }

    /// <summary>
    /// Doctor ↔ patient link check (any session status). patientIds / doctorIds — comma-separated.
    /// </summary>
    [HttpGet("doctor-patient-link")]
    public async Task<ActionResult<object>> DoctorPatientLink(
        [FromQuery] string doctorIds,
        [FromQuery] string patientIds,
        CancellationToken cancellationToken)
    {
        static Guid[] Parse(string? raw) =>
            (raw ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .Distinct()
                .ToArray();

        var doctors = Parse(doctorIds);
        var patients = Parse(patientIds);
        if (doctors.Length == 0 || patients.Length == 0)
            return BadRequest(new { error = "doctorIds and patientIds are required." });

        var linked = await _sessions.ExistsForDoctorsAndPatientsAsync(doctors, patients, cancellationToken);
        return Ok(new { linked });
    }

    /// <summary>
    /// Sessions of one doctor (all of their identity ids) inside a time window, for calendar enrichment.
    /// </summary>
    [HttpGet("doctors/sessions")]
    public async Task<ActionResult<IReadOnlyList<DoctorCalendarSessionDto>>> GetDoctorSessions(
        [FromQuery] string doctorIds,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] DateTime? openSince,
        CancellationToken cancellationToken = default)
    {
        var ids = (doctorIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => Guid.TryParse(part, out var id) ? id : Guid.Empty)
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToList();

        if (ids.Count == 0)
            return BadRequest(new { error = "doctorIds is required." });

        if (to <= from)
            return BadRequest(new { error = "to must be greater than from." });

        var sessions = await _sessions.GetByDoctorInRangeAsync(
            ids,
            ToUtc(from),
            ToUtc(to),
            openSince.HasValue ? ToUtc(openSince.Value) : null,
            cancellationToken);

        var terminalStatuses = new[] { "Completed", "Cancelled", "Expired" };

        return Ok(sessions.Select(s => new DoctorCalendarSessionDto
        {
            SessionId = s.Id,
            PatientId = s.PatientId,
            DoctorId = s.DoctorId,
            Type = s.Type.ToString(),
            Status = s.Status.ToString(),
            IsOpen = !terminalStatuses.Contains(s.Status.ToString()),
            UrgencyLevel = s.UrgencyLevel,
            ExpectedDurationMinutes = s.ExpectedDurationMinutes,
            TriageSessionId = s.TriageSessionId,
            RoutingDecisionId = s.RoutingDecisionId,
            ScheduledSlotId = s.ScheduledSlotId,
            ScheduledAt = s.ScheduledAt ?? s.StartedAt ?? s.CreatedAt,
            CreatedAt = s.CreatedAt,
            StartedAt = s.StartedAt,
            CompletedAt = s.CompletedAt,
            LastActivityAt = s.LastActivityAt,
            DoctorUnreadCount = s.DoctorUnreadCount,
            VideoRoomId = s.VideoRoomId
        }).ToList());
    }

    /// <summary>Столбцы времени — timestamptz, Npgsql отклоняет Unspecified из query string.</summary>
    private static DateTime ToUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}
