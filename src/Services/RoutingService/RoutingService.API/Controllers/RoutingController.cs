using System.Text.Json;
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
    private readonly IPatientRouteRepository _routes;

    public RoutingController(IRoutingDecisionRepository decisions, IPatientRouteRepository routes)
    {
        _decisions = decisions;
        _routes = routes;
    }

    [HttpGet("decisions/{decisionId:guid}")]
    [ProducesResponseType(typeof(RoutingDecisionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoutingDecisionResponse>> GetDecision(Guid decisionId, CancellationToken cancellationToken)
    {
        var decision = await _decisions.GetByIdAsync(decisionId, cancellationToken);
        if (decision is null)
            return NotFound();

        var labs = JsonSerializer.Deserialize<List<string>>(decision.RecommendedLabsJson) ?? [];

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
        var route = await _routes.GetActiveByPatientIdAsync(patientId, cancellationToken);
        if (route is null)
            return NotFound();

        var steps = JsonSerializer.Deserialize<List<RouteStepDto>>(route.StepsJson) ?? [];

        return Ok(new PatientActiveRouteResponse
        {
            RouteId = route.Id,
            PatientId = route.PatientId,
            Status = route.Status,
            CurrentStep = route.CurrentStep,
            TotalSteps = route.TotalSteps,
            Steps = steps
        });
    }
}
