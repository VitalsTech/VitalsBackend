using MedicalRecordService.API.Infrastructure;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.API.Controllers;

[ApiController]
[Route("internal/medical-records/patients/{patientId:guid}")]
public class InternalController : ControllerBase
{
    private readonly IMedicalEventService _events;
    private readonly IPatientRecordQueryService _queries;
    private readonly IAccessGrantService _grants;
    private readonly IOptions<JwtValidationOptions> _jwtOptions;

    public InternalController(
        IMedicalEventService events,
        IPatientRecordQueryService queries,
        IAccessGrantService grants,
        IOptions<JwtValidationOptions> jwtOptions)
    {
        _events = events;
        _queries = queries;
        _grants = grants;
        _jwtOptions = jwtOptions;
    }

    [HttpPost("events")]
    public async Task<ActionResult<AppendEventResponse>> AppendEvent(
        Guid patientId,
        [FromBody] AppendEventRequest request,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        actor.IsSystemService = true;
        actor.ServiceName = request.SourceService;
        var result = await _events.AppendEventAsync(patientId, request, actor, cancellationToken);
        return Ok(result);
    }

    [HttpPost("access-grants")]
    public async Task<ActionResult<AccessGrantDto>> CreateAccessGrant(
        Guid patientId,
        [FromBody] CreateAccessGrantRequest request,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        actor.IsSystemService = true;
        actor.ServiceName = "consultation-service";
        var result = await _grants.CreateGrantAsync(patientId, request, actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("state")]
    public async Task<ActionResult<PatientCurrentStateDto>> GetState(Guid patientId, CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        actor.IsSystemService = true;
        actor.ServiceName = "internal";
        return Ok(await _queries.GetCurrentStateAsync(patientId, actor, cancellationToken));
    }

    [HttpGet("history")]
    public async Task<ActionResult<PatientHistoryResponse>> GetHistory(
        Guid patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventTypes,
        CancellationToken cancellationToken)
    {
        var types = string.IsNullOrWhiteSpace(eventTypes)
            ? null
            : eventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        actor.IsSystemService = true;
        actor.ServiceName = "internal";
        return Ok(await _queries.GetHistoryAsync(patientId, from, to, types, actor, cancellationToken));
    }
}
