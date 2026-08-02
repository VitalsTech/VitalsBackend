using ApiGateway.Application.DTOs.Prescriptions;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/prescriptions")]
[Authorize]
public sealed class PrescriptionsController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public PrescriptionsController(IBackendForwarder backend) => _backend = backend;

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Create([FromBody] CreatePrescriptionRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, "api/prescriptions", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{prescriptionId:guid}")]
    public Task<IActionResult> Get(Guid prescriptionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/prescriptions/{prescriptionId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("patients/{patientId:guid}")]
    public Task<IActionResult> GetByPatient(Guid patientId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/prescriptions/patients/{patientId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{prescriptionId:guid}/validate")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Validate(Guid prescriptionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Post, $"api/prescriptions/{prescriptionId}/validate", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{prescriptionId:guid}/sign")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Sign(Guid prescriptionId, [FromQuery] bool confirmWarnings = false, CancellationToken cancellationToken = default) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Post, $"api/prescriptions/{prescriptionId}/sign?confirmWarnings={confirmWarnings.ToString().ToLowerInvariant()}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{prescriptionId:guid}/send-to-pharmacy")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> SendToPharmacy(Guid prescriptionId, [FromBody] SendToPharmacyRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, $"api/prescriptions/{prescriptionId}/send-to-pharmacy", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{prescriptionId:guid}/cancel")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Cancel(Guid prescriptionId, [FromBody] CancelPrescriptionRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, $"api/prescriptions/{prescriptionId}/cancel", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{prescriptionId:guid}/qr")]
    public Task<IActionResult> GetQr(Guid prescriptionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/prescriptions/{prescriptionId}/qr", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("{prescriptionId:guid}/instructions")]
    public Task<IActionResult> GetInstructions(Guid prescriptionId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/prescriptions/{prescriptionId}/instructions", ForwardContext, cancellationToken: cancellationToken), cancellationToken);
}
