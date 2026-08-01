using ApiGateway.Application.Consultations;
using ApiGateway.Application.DTOs.Consultations;
using ApiGateway.Application.DTOs.Doctors;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/consultations")]
[Authorize]
public sealed class ConsultationsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;
    private readonly IDoctorCalendarService _calendar;

    public ConsultationsController(IBackendForwarder backend, IDoctorCalendarService calendar)
    {
        _backend = backend;
        _calendar = calendar;
    }

    [HttpPost]
    public Task<IActionResult> Create([FromBody] CreateConsultationRequestDto request, CancellationToken cancellationToken)
    {
        var urgencyLevel = request.UrgencyLevel is < 1 or > 5 ? 3 : request.UrgencyLevel;
        var backendRequest = new
        {
            request.PatientId,
            request.DoctorId,
            request.DoctorName,
            ConsultationType = ConsultationTypeNormalizer.Normalize(request.ConsultationType),
            UrgencyLevel = urgencyLevel,
            request.RoutingDecisionId,
            request.TriageSessionId
        };
        return Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, "api/consultations", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    /// <summary>
    /// Запись пациента на слот расписания врача: резервирует слот и создаёт консультацию на его время.
    /// </summary>
    [HttpPost("book")]
    [Authorize(Roles = "Patient")]
    [ProducesResponseType(typeof(BookConsultationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<BookConsultationResponseDto>> Book(
        [FromBody] BookConsultationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (CurrentUserId is not { } patientId)
            return Unauthorized(new { error = "Неверный идентификатор пользователя." });

        if (request.DoctorId == Guid.Empty || request.SlotId == Guid.Empty)
            return BadRequest(new { error = "doctorId и slotId обязательны." });

        var result = await _calendar.BookAsync(
            patientId,
            new BookConsultationRequestDto
            {
                DoctorId = request.DoctorId,
                SlotId = request.SlotId,
                ConsultationType = ConsultationTypeNormalizer.Normalize(request.ConsultationType),
                UrgencyLevel = request.UrgencyLevel is < 1 or > 5 ? 3 : request.UrgencyLevel,
                TriageSessionId = request.TriageSessionId
            },
            ForwardContext,
            cancellationToken);

        return result.Outcome switch
        {
            BookConsultationOutcome.Booked => Ok(result.Booking),
            BookConsultationOutcome.SlotUnavailable => Conflict(new { error = result.Error }),
            BookConsultationOutcome.DoctorNotFound => NotFound(new { error = result.Error }),
            _ => StatusCode(StatusCodes.Status502BadGateway, new { error = result.Error })
        };
    }

    /// <summary>
    /// Список консультаций текущего пользователя (пациент — свои записи, врач — свои приёмы).
    /// </summary>
    [HttpGet("mine")]
    public Task<IActionResult> ListMine(
        [FromQuery] bool includeCompleted = false,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var path =
            $"api/consultations/mine?includeCompleted={includeCompleted.ToString().ToLowerInvariant()}" +
            $"&limit={Math.Clamp(limit, 1, 100)}";
        return Forward(
            _backend.ForwardAsync("consultation", HttpMethod.Get, path, ForwardContext, cancellationToken: cancellationToken),
            cancellationToken);
    }

    [HttpGet("active")]
    public Task<IActionResult> GetActive(
        [FromQuery] Guid patientId,
        [FromQuery] Guid doctorId,
        CancellationToken cancellationToken) =>
        Forward(
            _backend.ForwardAsync(
                "consultation",
                HttpMethod.Get,
                $"api/consultations/active?patientId={patientId}&doctorId={doctorId}",
                ForwardContext,
                cancellationToken: cancellationToken),
            cancellationToken);

    [HttpGet("{sessionId:guid}")]
    public Task<IActionResult> Get(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Get, $"api/consultations/{sessionId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/join")]
    public Task<IActionResult> Join(Guid sessionId, [FromBody] JoinSessionRequestDto? request, CancellationToken cancellationToken)
    {
        if (!TryResolveParticipantRole(User, request?.Role, out var role, out var error))
            return Task.FromResult<IActionResult>(Forbid());

        var backendRequest = new { role };
        return Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/join", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    private static bool TryResolveParticipantRole(
        ClaimsPrincipal user,
        string? requestedRole,
        out string role,
        out string? error)
    {
        error = null;
        var isDoctor = user.IsInRole("Doctor") ||
                       user.FindAll("role").Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase)) ||
                       user.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase));
        var isPatient = user.IsInRole("Patient") ||
                        user.FindAll("role").Any(c => c.Value.Equals("Patient", StringComparison.OrdinalIgnoreCase)) ||
                        user.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals("Patient", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(requestedRole) &&
            requestedRole.Equals("Doctor", StringComparison.OrdinalIgnoreCase) &&
            !isDoctor)
        {
            role = "Patient";
            error = "Patient token cannot join as Doctor.";
            return false;
        }

        if (isDoctor && requestedRole?.Equals("Doctor", StringComparison.OrdinalIgnoreCase) == true)
        {
            role = "Doctor";
            return true;
        }

        if (isDoctor && !isPatient)
        {
            role = "Doctor";
            return true;
        }

        role = "Patient";
        return true;
    }

    [HttpPost("{sessionId:guid}/consent")]
    [Authorize(Roles = "Patient")]
    public Task<IActionResult> Consent(Guid sessionId, [FromBody] ConsentRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/consent", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{sessionId:guid}/messages")]
    public Task<IActionResult> GetMessages(
        Guid sessionId,
        [FromQuery] long afterSequence = 0,
        [FromQuery] bool markAsRead = true,
        CancellationToken cancellationToken = default)
    {
        var path = $"api/consultations/{sessionId}/messages?afterSequence={afterSequence}&markAsRead={markAsRead.ToString().ToLowerInvariant()}";
        return Forward(_backend.ForwardAsync("consultation", HttpMethod.Get, path, ForwardContext, cancellationToken: cancellationToken), cancellationToken);
    }

    [HttpPost("{sessionId:guid}/messages/read")]
    public Task<IActionResult> MarkMessagesRead(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/messages/read", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/messages")]
    public Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/messages", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/pause")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Pause(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/pause", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/resume")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Resume(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/resume", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/doctor-leave")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> DoctorLeave(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/doctor-leave", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/complete")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Complete(Guid sessionId, [FromBody] CompleteConsultationRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/complete", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/confirm")]
    [Authorize(Roles = "Patient")]
    public Task<IActionResult> Confirm(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/confirm", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/cancel")]
    public Task<IActionResult> Cancel(Guid sessionId, [FromBody] CancelSessionRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/cancel", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/video/start")]
    public Task<IActionResult> StartVideo(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/video/start", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/emergency")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Emergency(Guid sessionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/emergency", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{sessionId:guid}/ratings")]
    public Task<IActionResult> Rate(Guid sessionId, [FromBody] SubmitRatingRequestDto request, CancellationToken cancellationToken)
    {
        var role = User.IsInRole("Doctor") ||
                   User.FindAll("role").Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase)) ||
                   User.FindAll(ClaimTypes.Role).Any(c => c.Value.Equals("Doctor", StringComparison.OrdinalIgnoreCase))
            ? "Doctor"
            : "Patient";

        var backendRequest = new
        {
            Role = role,
            request.Score,
            request.Feedback,
            request.ClarityScore,
            request.TimelinessScore,
            request.ProblemResolved
        };
        return Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/ratings", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    [HttpPost("{sessionId:guid}/invite-doctor")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> InviteDoctor(Guid sessionId, [FromBody] InviteDoctorRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("consultation", HttpMethod.Post, $"api/consultations/{sessionId}/invite-doctor", ForwardContext, request, cancellationToken), cancellationToken);
}
