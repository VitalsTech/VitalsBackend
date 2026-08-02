using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace RoutingService.API.Controllers;

[ApiController]
[Route("api/routing")]
[Authorize]
public sealed class RoutingController : ControllerBase
{
    private readonly IRoutingDecisionRepository _decisions;
    private readonly IRoutingOrchestrator _orchestrator;

    public RoutingController(IRoutingDecisionRepository decisions, IRoutingOrchestrator orchestrator)
    {
        _decisions = decisions;
        _orchestrator = orchestrator;
    }

    [HttpGet("decisions/{decisionId:guid}")]
    [ProducesResponseType(typeof(RoutingDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutingDecisionResponse>> GetDecision(Guid decisionId, CancellationToken cancellationToken)
    {
        var decision = await _decisions.GetByIdAsync(decisionId, cancellationToken);
        if (decision is null)
            return NotFound();

        var labs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(decision.RecommendedLabsJson) ?? [];

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
            RecommendedLabs = labs,
            PatientMessage = decision.PatientMessage,
            Rationale = decision.Rationale,
            AlgorithmVersion = decision.AlgorithmVersion,
            IsFallback = decision.IsFallback
        });
    }

    [HttpGet("patients/{patientId:guid}/active-route")]
    [ProducesResponseType(typeof(PatientActiveRouteResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatientActiveRouteResponse>> GetActiveRoute(Guid patientId, CancellationToken cancellationToken)
    {
        var route = await _orchestrator.GetActiveRouteAsync(patientId, cancellationToken);
        if (route is null)
            return NotFound();

        return Ok(route);
    }
}
