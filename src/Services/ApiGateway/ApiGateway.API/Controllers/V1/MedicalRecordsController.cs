using System.Text.Json;
using ApiGateway.Application.DTOs.MedicalRecords;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/medical-records/patients/{patientId:guid}")]
[Authorize]
public sealed class MedicalRecordsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public MedicalRecordsController(IBackendForwarder backend) => _backend = backend;

    [HttpPost("events")]
    [ProducesResponseType(typeof(AppendEventResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> AppendEvent(Guid patientId, [FromBody] AppendEventRequestDto request, CancellationToken cancellationToken)
    {
        var backendRequest = new
        {
            eventId = Guid.NewGuid(),
            eventType = request.EventType,
            sourceService = request.SourceService,
            payload = ParsePayload(request.PayloadJson)
        };
        return Forward(_backend.ForwardJsonAsync("medical", HttpMethod.Post, $"api/medical-records/patients/{patientId}/events", ForwardContext, backendRequest, cancellationToken), cancellationToken);
    }

    private static JsonElement ParsePayload(string payloadJson)
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
            return JsonSerializer.SerializeToElement(new { });

        try
        {
            using var document = JsonDocument.Parse(payloadJson);
            return document.RootElement.Clone();
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(payloadJson);
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(PatientHistoryResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetHistory(
        Guid patientId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? eventTypes,
        CancellationToken cancellationToken)
    {
        var query = new List<string>();
        if (from.HasValue) query.Add($"from={Uri.EscapeDataString(from.Value.ToString("O"))}");
        if (to.HasValue) query.Add($"to={Uri.EscapeDataString(to.Value.ToString("O"))}");
        if (!string.IsNullOrWhiteSpace(eventTypes)) query.Add($"eventTypes={Uri.EscapeDataString(eventTypes)}");
        var suffix = query.Count == 0 ? string.Empty : "?" + string.Join('&', query);
        return Forward(_backend.ForwardAsync("medical", HttpMethod.Get, $"api/medical-records/patients/{patientId}/history{suffix}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
    }

    [HttpGet("state")]
    [ProducesResponseType(typeof(PatientCurrentStateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> GetState(Guid patientId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("medical", HttpMethod.Get, $"api/medical-records/patients/{patientId}/state", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("access-grants")]
    public Task<IActionResult> CreateAccessGrant(Guid patientId, [FromBody] CreateAccessGrantRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("medical", HttpMethod.Post, $"api/medical-records/patients/{patientId}/access-grants", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpDelete("access-grants/{grantId:guid}")]
    public Task<IActionResult> RevokeAccessGrant(Guid patientId, Guid grantId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("medical", HttpMethod.Delete, $"api/medical-records/patients/{patientId}/access-grants/{grantId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("attachments")]
    public Task<IActionResult> ListAttachments(Guid patientId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("medical", HttpMethod.Get, $"api/medical-records/patients/{patientId}/attachments", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
