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
}
