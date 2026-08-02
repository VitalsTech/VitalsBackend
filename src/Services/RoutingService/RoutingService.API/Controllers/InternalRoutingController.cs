using System.Text.Json;
using RoutingService.Application.DTOs;
using RoutingService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace RoutingService.API.Controllers;

[ApiController]
[Route("internal/routing")]
public sealed class InternalRoutingController : ControllerBase
{
    private readonly IRoutingOrchestrator _orchestrator;
    private readonly IRoutingDecisionRepository _decisions;

    public InternalRoutingController(IRoutingOrchestrator orchestrator, IRoutingDecisionRepository decisions)
    {
        _orchestrator = orchestrator;
        _decisions = decisions;
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

    /// <summary>Анализы из протокола консультации → шаг маршрута + recommendedLabs в decision.</summary>
    [HttpPost("patients/{patientId:guid}/labs")]
    [ProducesResponseType(typeof(PatientActiveRouteResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientActiveRouteResponse>> AppendPostConsultationLabs(
        Guid patientId,
        [FromBody] AppendPostConsultationLabsRequest request,
        CancellationToken cancellationToken)
    {
        var route = await _orchestrator.AppendPostConsultationLabsAsync(patientId, request, cancellationToken);
        return Ok(route);
    }
}
