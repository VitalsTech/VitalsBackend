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
    private readonly IMedicalRecordContextClient _medicalRecord;

    public TriageController(ITriageOrchestrator triage, IMedicalRecordContextClient medicalRecord)
    {
        _triage = triage;
        _medicalRecord = medicalRecord;
    }

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
    /// Завершить триаж и записать маршрут в медкарту (mock при отсутствии оценки LLM).
    /// </summary>
    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<TriageSessionResponse>> CompleteSession(
        Guid sessionId,
        CancellationToken cancellationToken) =>
        Ok(await _triage.CompleteSessionAsync(sessionId, cancellationToken));

    private async Task<bool> CanAccessPatientAsync(Guid patientId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        if (ids.Contains(patientId))
            return true;

        if (!UserClaims.IsInAppRole(User, "Doctor"))
            return false;

        return await _medicalRecord.DoctorHasAccessAsync(patientId, ids, cancellationToken);
    }
}

[ApiController]
[Authorize]
[Route("api/triage/patients/{patientId:guid}/sessions")]
public sealed class PatientTriageSessionsController : ControllerBase
{
    private readonly ITriageOrchestrator _triage;
    private readonly IMedicalRecordContextClient _medicalRecord;

    public PatientTriageSessionsController(
        ITriageOrchestrator triage,
        IMedicalRecordContextClient medicalRecord)
    {
        _triage = triage;
        _medicalRecord = medicalRecord;
    }

    /// <summary>
    /// Список сессий триажа пациента (для врача с grant/консультацией или самого пациента).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TriageSessionResponse>>> ListSessions(
        Guid patientId,
        [FromQuery] int limit = 5,
        CancellationToken cancellationToken = default)
    {
        var ids = UserClaims.GetIdentityIds(User);
        var isPatientSelf = ids.Contains(patientId);
        var isDoctor = UserClaims.IsInAppRole(User, "Doctor");

        if (!isPatientSelf)
        {
            if (!isDoctor)
                return Forbid();

            var allowed = await _medicalRecord.DoctorHasAccessAsync(patientId, ids, cancellationToken);
            if (!allowed)
                return Forbid();
        }

        var sessions = await _triage.GetSessionsByPatientAsync(patientId, limit, cancellationToken);
        return Ok(sessions);
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
        CancellationToken cancellationToken = default) =>
        Ok(await _triage.GetSessionsByPatientAsync(patientId, limit, cancellationToken));
}
