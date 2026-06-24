using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationService.API.Controllers;

[ApiController]
[Route("internal/consultations")]
public sealed class InternalConsultationController : ControllerBase
{
    private readonly IConsultationService _consultations;

    public InternalConsultationController(IConsultationService consultations) => _consultations = consultations;

    [HttpPost("routing-decision")]
    public async Task<ActionResult<ConsultationSessionResponse>> CreateFromRoutingDecision(
        [FromBody] RoutingDecisionEventDto request,
        CancellationToken cancellationToken)
    {
        if (request.OutcomeType != "Consultation" && request.OutcomeType != "LabsBeforeConsultation")
            return BadRequest(new { error = "Only consultation outcomes create sessions." });

        return Ok(await _consultations.CreateFromRoutingDecisionAsync(request, cancellationToken));
    }
}
