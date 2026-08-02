using MedicalRecordService.API.Infrastructure;
using MedicalRecordService.Application.DTOs;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using MedicalRecordService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.API.Controllers;

[ApiController]
[Authorize]
[Route("api/medical-records/patients/{patientId:guid}")]
public class MedicalRecordsController : ControllerBase
{
    private readonly IMedicalEventService _events;
    private readonly IPatientRecordQueryService _queries;
    private readonly IAccessGrantService _grants;
    private readonly IPatientAttachmentService _attachments;
    private readonly IOptions<JwtValidationOptions> _jwtOptions;

    public MedicalRecordsController(
        IMedicalEventService events,
        IPatientRecordQueryService queries,
        IAccessGrantService grants,
        IPatientAttachmentService attachments,
        IOptions<JwtValidationOptions> jwtOptions)
    {
        _events = events;
        _queries = queries;
        _grants = grants;
        _attachments = attachments;
        _jwtOptions = jwtOptions;
    }

    [HttpPost("events")]
    public async Task<ActionResult<AppendEventResponse>> AppendEvent(
        Guid patientId,
        [FromBody] AppendEventRequest request,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        var result = await _events.AppendEventAsync(patientId, request, actor, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// История событий медкарты.
    /// eventTypes — через запятую, например DiagnosisConfirmed,DocumentUploaded.
    /// Алиас document → DocumentUploaded.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType(typeof(PatientHistoryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<PatientHistoryResponse>> GetHistory(
        Guid patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventTypes,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<string>? types = null;
        if (!string.IsNullOrWhiteSpace(eventTypes))
        {
            types = MedicalEventTypes.NormalizeFilter(
                eventTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        var result = await _queries.GetHistoryAsync(patientId, from, to, types, actor, cancellationToken);
        return Ok(result);
    }

    [HttpGet("state")]
    public async Task<ActionResult<PatientCurrentStateDto>> GetState(Guid patientId, CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        var result = await _queries.GetCurrentStateAsync(patientId, actor, cancellationToken);
        return Ok(result);
    }

    [HttpPost("access-grants")]
    public async Task<ActionResult<AccessGrantDto>> CreateAccessGrant(
        Guid patientId,
        [FromBody] CreateAccessGrantRequest request,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        var result = await _grants.CreateGrantAsync(patientId, request, actor, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("access-grants/{grantId:guid}")]
    public async Task<IActionResult> RevokeAccessGrant(Guid patientId, Guid grantId, CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        await _grants.RevokeGrantAsync(patientId, grantId, actor, cancellationToken);
        return NoContent();
    }

    [HttpPost("attachments")]
    [RequestSizeLimit(52_428_800)]
    public async Task<ActionResult<PatientAttachmentDto>> UploadAttachment(
        Guid patientId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        await using var stream = file.OpenReadStream();
        var result = await _attachments.UploadAsync(
            patientId,
            file.FileName,
            stream,
            file.ContentType,
            actor,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("attachments")]
    public async Task<ActionResult<IReadOnlyList<PatientAttachmentDto>>> ListAttachments(
        Guid patientId,
        CancellationToken cancellationToken)
    {
        var actor = ActorContextFactory.FromHttpContext(HttpContext, _jwtOptions);
        return Ok(await _attachments.ListAsync(patientId, actor, cancellationToken));
    }
}
