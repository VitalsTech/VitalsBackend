using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AITriageService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/triage/sessions")]
public sealed class TriageController : ControllerBase
{
    private readonly ITriageOrchestrator _triage;

    public TriageController(ITriageOrchestrator triage) => _triage = triage;

    [HttpPost]
    public async Task<ActionResult<TriageSessionResponse>> CreateSession(
        [FromBody] CreateTriageSessionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _triage.CreateSessionAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSession), new { sessionId = result.SessionId }, result);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<TriageSessionResponse>> GetSession(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _triage.GetSessionAsync(sessionId, cancellationToken));

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<ActionResult<TriageSessionResponse>> SendMessage(
        Guid sessionId,
        [FromBody] SendTriageMessageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _triage.ProcessMessageAsync(sessionId, request, cancellationToken));
}

[ApiController]
[Route("internal/triage")]
public sealed class InternalTriageController : ControllerBase
{
    private readonly ITriageOrchestrator _triage;

    public InternalTriageController(ITriageOrchestrator triage) => _triage = triage;

    [HttpGet("sessions/{sessionId:guid}")]
    public async Task<ActionResult<TriageSessionResponse>> GetSession(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _triage.GetSessionAsync(sessionId, cancellationToken));
}
