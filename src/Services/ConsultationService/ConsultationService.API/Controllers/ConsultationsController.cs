using ConsultationService.API.Infrastructure;
using ConsultationService.Application.DTOs;
using ConsultationService.Application.Interfaces;
using ConsultationService.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ConsultationService.API.Controllers;

[ApiController]
[Route("api/consultations")]
[Authorize]
public sealed class ConsultationsController : ControllerBase
{
    private readonly IConsultationService _consultations;

    public ConsultationsController(IConsultationService consultations) => _consultations = consultations;

    [HttpPost]
    public async Task<ActionResult<ConsultationSessionResponse>> Create(
        [FromBody] CreateConsultationRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _consultations.OpenOrCreateAsync(request, cancellationToken));

    [HttpGet("active")]
    public async Task<ActionResult<ConsultationSessionResponse>> GetActive(
        [FromQuery] Guid patientId,
        [FromQuery] Guid doctorId,
        CancellationToken cancellationToken)
    {
        var session = await _consultations.FindActiveAsync(patientId, doctorId, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ConsultationSessionResponse>> Get(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _consultations.GetSessionAsync(sessionId, cancellationToken));

    [HttpPost("{sessionId:guid}/join")]
    public async Task<ActionResult<ConsultationSessionResponse>> Join(
        Guid sessionId,
        [FromBody] JoinSessionRequest? request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        var userId = ids[0];
        var roleFromToken = UserClaims.GetParticipantRole(User);

        if (request is not null &&
            !string.IsNullOrWhiteSpace(request.Role) &&
            request.Role.Equals("Doctor", StringComparison.OrdinalIgnoreCase) &&
            roleFromToken != ParticipantRole.Doctor)
        {
            return Forbid();
        }

        var role = roleFromToken;
        if (roleFromToken == ParticipantRole.Doctor &&
            request?.Role?.Equals("Patient", StringComparison.OrdinalIgnoreCase) == true)
        {
            role = ParticipantRole.Patient;
        }

        return Ok(await _consultations.JoinAsync(sessionId, userId, role, ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/consent")]
    public async Task<ActionResult<ConsultationSessionResponse>> Consent(
        Guid sessionId,
        [FromBody] ConsentRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.RecordConsentAsync(sessionId, ids[0], request, ids, cancellationToken));
    }

    [HttpGet("{sessionId:guid}/messages")]
    public async Task<ActionResult<IReadOnlyList<ConsultationMessageDto>>> GetMessages(
        Guid sessionId,
        [FromQuery] long afterSequence = 0,
        CancellationToken cancellationToken = default) =>
        Ok(await _consultations.GetMessagesAsync(sessionId, afterSequence, cancellationToken));

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<ActionResult<ConsultationMessageDto>> SendMessage(
        Guid sessionId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        var role = UserClaims.GetParticipantRole(User);
        return Ok(await _consultations.SendMessageAsync(sessionId, ids[0], role, request, ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/pause")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> Pause(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.PauseAsync(sessionId, ids[0], ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/resume")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> Resume(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.ResumeAsync(sessionId, ids[0], ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/doctor-leave")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> DoctorLeave(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.DoctorLeaveAsync(sessionId, ids[0], ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/complete")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> Complete(
        Guid sessionId,
        [FromBody] CompleteConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.CompleteAsync(sessionId, ids[0], request, ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/confirm")]
    public async Task<ActionResult<ConsultationSessionResponse>> Confirm(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.PatientConfirmAsync(sessionId, ids[0], ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/cancel")]
    public async Task<ActionResult<ConsultationSessionResponse>> Cancel(
        Guid sessionId,
        [FromBody] CancelSessionRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.CancelAsync(sessionId, ids[0], request.Reason, ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/video/start")]
    public async Task<ActionResult<VideoRoomResponse>> StartVideo(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.StartVideoAsync(sessionId, ids[0], ids, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/emergency")]
    [Authorize(Roles = "Doctor")]
    public async Task<IActionResult> Emergency(Guid sessionId, CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        await _consultations.TriggerEmergencyAsync(sessionId, ids[0], cancellationToken);
        return Accepted();
    }

    [HttpPost("{sessionId:guid}/ratings")]
    public async Task<IActionResult> Rate(
        Guid sessionId,
        [FromBody] SubmitRatingRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        var role = UserClaims.GetParticipantRole(User);
        var normalized = new SubmitRatingRequest
        {
            Role = role.ToString(),
            Score = request.Score,
            Feedback = request.Feedback,
            ClarityScore = request.ClarityScore,
            TimelinessScore = request.TimelinessScore,
            ProblemResolved = request.ProblemResolved
        };
        await _consultations.SubmitRatingAsync(sessionId, ids[0], normalized, cancellationToken);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/invite-doctor")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> InviteDoctor(
        Guid sessionId,
        [FromBody] InviteDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var ids = UserClaims.GetIdentityIds(User);
        return Ok(await _consultations.InviteDoctorAsync(sessionId, ids[0], request, cancellationToken));
    }
}
