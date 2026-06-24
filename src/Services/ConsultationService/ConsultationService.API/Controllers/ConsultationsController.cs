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
        Ok(await _consultations.CreateManualAsync(request, cancellationToken));

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ConsultationSessionResponse>> Get(Guid sessionId, CancellationToken cancellationToken) =>
        Ok(await _consultations.GetSessionAsync(sessionId, cancellationToken));

    [HttpPost("{sessionId:guid}/join")]
    public async Task<ActionResult<ConsultationSessionResponse>> Join(
        Guid sessionId,
        [FromBody] JoinSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        var role = Enum.Parse<ParticipantRole>(request.Role, true);
        return Ok(await _consultations.JoinAsync(sessionId, userId, role, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/consent")]
    public async Task<ActionResult<ConsultationSessionResponse>> Consent(
        Guid sessionId,
        [FromBody] ConsentRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.RecordConsentAsync(sessionId, userId, request, cancellationToken));
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
        var userId = UserClaims.GetUserId(User);
        var roleClaim = User.FindFirst("role")?.Value ?? "patient";
        var role = roleClaim.Contains("doctor", StringComparison.OrdinalIgnoreCase)
            ? ParticipantRole.Doctor
            : ParticipantRole.Patient;
        return Ok(await _consultations.SendMessageAsync(sessionId, userId, role, request, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/pause")]
    public async Task<ActionResult<ConsultationSessionResponse>> Pause(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.PauseAsync(sessionId, userId, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/resume")]
    public async Task<ActionResult<ConsultationSessionResponse>> Resume(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.ResumeAsync(sessionId, userId, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/doctor-leave")]
    public async Task<ActionResult<ConsultationSessionResponse>> DoctorLeave(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.DoctorLeaveAsync(sessionId, userId, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/complete")]
    public async Task<ActionResult<ConsultationSessionResponse>> Complete(
        Guid sessionId,
        [FromBody] CompleteConsultationRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.CompleteAsync(sessionId, userId, request, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/confirm")]
    public async Task<ActionResult<ConsultationSessionResponse>> Confirm(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.PatientConfirmAsync(sessionId, userId, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/cancel")]
    public async Task<ActionResult<ConsultationSessionResponse>> Cancel(
        Guid sessionId,
        [FromBody] CancelSessionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.CancelAsync(sessionId, userId, request.Reason, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/video/start")]
    public async Task<ActionResult<VideoRoomResponse>> StartVideo(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.StartVideoAsync(sessionId, userId, cancellationToken));
    }

    [HttpPost("{sessionId:guid}/emergency")]
    public async Task<IActionResult> Emergency(Guid sessionId, CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        await _consultations.TriggerEmergencyAsync(sessionId, userId, cancellationToken);
        return Accepted();
    }

    [HttpPost("{sessionId:guid}/ratings")]
    public async Task<IActionResult> Rate(
        Guid sessionId,
        [FromBody] SubmitRatingRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        await _consultations.SubmitRatingAsync(sessionId, userId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{sessionId:guid}/invite-doctor")]
    public async Task<ActionResult<ConsultationSessionResponse>> InviteDoctor(
        Guid sessionId,
        [FromBody] InviteDoctorRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _consultations.InviteDoctorAsync(sessionId, userId, request, cancellationToken));
    }
}
