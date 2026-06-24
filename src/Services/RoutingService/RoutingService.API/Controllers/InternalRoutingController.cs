using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace RoutingService.API.Controllers;

[ApiController]
[Route("internal/routing")]
public sealed class InternalRoutingController : ControllerBase
{
    private readonly IRoutingOrchestrator _orchestrator;

    public InternalRoutingController(IRoutingOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("triage-completed")]
    [ProducesResponseType(typeof(RoutingDecisionResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<RoutingDecisionResponse>> ProcessTriageCompleted(
        [FromBody] TriageCompletedEventDto request,
        CancellationToken cancellationToken)
    {
        var result = await _orchestrator.ProcessTriageCompletedAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("decisions/{decisionId:guid}")]
    [ProducesResponseType(typeof(RoutingDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutingDecisionResponse>> GetDecision(Guid decisionId, CancellationToken cancellationToken)
    {
        var decision = await HttpContext.RequestServices
            .GetRequiredService<IRoutingDecisionRepository>()
            .GetByIdAsync(decisionId, cancellationToken);

        if (decision is null)
            return NotFound();

        return Ok(new RoutingDecisionResponse
        {
            DecisionId = decision.Id,
            PatientId = decision.PatientId,
            TriageSessionId = decision.TriageSessionId,
            OutcomeType = decision.OutcomeType.ToString(),
            Specialist = decision.Specialist,
            ConsultationFormat = decision.ConsultationFormat?.ToString(),
            AssignedDoctorId = decision.AssignedDoctorId,
            AssignedDoctorName = decision.AssignedDoctorName,
            Priority = decision.Priority,
            UrgencyLevel = decision.UrgencyLevel,
            PatientMessage = decision.PatientMessage,
            Rationale = decision.Rationale,
            AlgorithmVersion = decision.AlgorithmVersion,
            IsFallback = decision.IsFallback
        });
    }
}
