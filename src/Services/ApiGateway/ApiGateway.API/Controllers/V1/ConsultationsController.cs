using ApiGateway.Application.DTOs.Consultations;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/consultations")]
[Authorize]
public sealed class ConsultationsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public ConsultationsController(IBackendForwarder backend) => _backend = backend;

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateConsultationRequestDto request, CancellationToken cancellationToken)
    {
        var backendRequest = new
        {
            request.PatientId,
            request.DoctorId,
            request.DoctorName,
            request.ConsultationType,
            request.UrgencyLevel,
            request.RoutingDecisionId,
            request.TriageSessionId
        };
        return Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, "api/consultations", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    [HttpGet("{sessionId:guid}")]
    public Task<IActionResult> Get(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Get, $"api/consultations/{sessionId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/join")]
    public Task<IActionResult> Join(Guid sessionId, [FromBody] JoinSessionRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/join", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/consent")]
    public Task<IActionResult> Consent(Guid sessionId, [FromBody] ConsentRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/consent", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{sessionId:guid}/messages")]
    public Task<IActionResult> GetMessages(Guid sessionId, [FromQuery] long afterSequence = 0, CancellationToken cancellationToken = default)
    {
        var path = $"api/consultations/{sessionId}/messages?afterSequence={afterSequence}";
        return Forward(_backend.ForwardAsync("consultation", HttpMethod.Get, path, ForwardContext, cancellationToken: cancellationToken), cancellationToken);
    }

    [HttpPost("{sessionId:guid}/messages")]
    public Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/messages", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/pause")]
    public Task<IActionResult> Pause(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/pause", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/resume")]
    public Task<IActionResult> Resume(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/resume", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/doctor-leave")]
    public Task<IActionResult> DoctorLeave(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/doctor-leave", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/complete")]
    public Task<IActionResult> Complete(Guid sessionId, [FromBody] CompleteConsultationRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/complete", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/confirm")]
    public Task<IActionResult> Confirm(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/confirm", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid sessionId, [FromBody] CancelSessionRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/cancel", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/video/start")]
    public Task<IActionResult> StartVideo(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/video/start", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/emergency")]
    public Task<IActionResult> Emergency(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/emergency", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/ratings")]
    public Task<IActionResult> Rate(Guid sessionId, [FromBody] SubmitRatingRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/ratings", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/invite-doctor")]
    public Task<IActionResult> InviteDoctor(Guid sessionId, [FromBody] InviteDoctorRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/invite-doctor", ForwardContext, request, cancellationToken), cancellationToken);
}
