using AITriageService.API.Infrastructure;
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
    public async Task<ActionResult<TriageSessionResponse>> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await _triage.GetSessionAsync(sessionId, cancellationToken);
        if (!await CanAccessPatientAsync(session.PatientId, cancellationToken))
            return Forbid();

        return Ok(session);
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<ActionResult<TriageSessionResponse>> SendMessage(
        Guid sessionId,
        [FromBody] SendTriageMessageRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _triage.ProcessMessageAsync(sessionId, request, cancellationToken));

    /// <summary>
    /// Завершить триаж: финальная оценка YandexGPT (если ещё не было) → routing + медкарта.
    /// </summary>
    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<TriageSessionResponse>> CompleteSession(
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Ok(await _triage.CompleteSessionAsync(sessionId, cancellationToken));

    private Task<bool> CanAccessPatientAsync(Guid patientId, CancellationToken _)
    {
        var ids = UserClaims.GetIdentityIds(User);
        if (ids.Contains(patientId))
            return Task.FromResult(true);

        // MVP: врач может читать триаж любого пациента (как state/history в медкарте).
        if (UserClaims.IsInAppRole(User, "Doctor"))
            return Task.FromResult(true);

        return Task.FromResult(false);
    }
}

[ApiController]
[Authorize]
[Route("api/triage/patients/{patientId:guid}/sessions")]
public sealed class PatientTriageSessionsController : ControllerBase
{
    private readonly ITriageOrchestrator _triage;

    public PatientTriageSessionsController(ITriageOrchestrator triage) => _triage = triage;

    /// <summary>
    /// Список сессий триажа пациента (для врача или самого пациента).
    /// <paramref name="alsoPatientIds"/> — доп. PublicId/ProfileId того же пациента (через запятую).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TriageSessionResponse>>> ListSessions(
        Guid patientId,
        [FromQuery] int limit = 5,
        [FromQuery] string? alsoPatientIds = null,
        CancellationToken cancellationToken = default)
    {
        var patientIds = ParsePatientIds(patientId, alsoPatientIds);
        var ids = UserClaims.GetIdentityIds(User);
        var isPatientSelf = patientIds.Any(ids.Contains);
        var isDoctor = UserClaims.IsInAppRole(User, "Doctor");

        if (!isPatientSelf && !isDoctor)
            return Forbid();

        var sessions = await _triage.GetSessionsByPatientsAsync(patientIds, limit, cancellationToken);
        return Ok(sessions);
    }

    private static List<Guid> ParsePatientIds(Guid patientId, string? alsoPatientIds)
    {
        var result = new HashSet<Guid> { patientId };
        if (!string.IsNullOrWhiteSpace(alsoPatientIds))
        {
            foreach (var part in alsoPatientIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var id) && id != Guid.Empty)
                    result.Add(id);
            }
        }

        return result.ToList();
    }
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

    [HttpGet("patients/{patientId:guid}/sessions")]
    public async Task<ActionResult<IReadOnlyList<TriageSessionResponse>>> GetPatientSessions(
        Guid patientId,
        [FromQuery] int limit = 1,
        [FromQuery] string? alsoPatientIds = null,
        CancellationToken cancellationToken = default)
    {
        var patientIds = new HashSet<Guid> { patientId };
        if (!string.IsNullOrWhiteSpace(alsoPatientIds))
        {
            foreach (var part in alsoPatientIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (Guid.TryParse(part, out var id) && id != Guid.Empty)
                    patientIds.Add(id);
            }
        }

        return Ok(await _triage.GetSessionsByPatientsAsync(patientIds.ToList(), limit, cancellationToken));
    }
}
