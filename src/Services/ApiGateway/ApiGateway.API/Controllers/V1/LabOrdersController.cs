using ApiGateway.Application.DTOs.Prescriptions;
using ApiGateway.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
[Route("api/v1/lab-orders")]
[Authorize]
public sealed class LabOrdersController : GatewayControllerBase
{
    private readonly IBackendForwarder _backend;

    public LabOrdersController(IBackendForwarder backend) => _backend = backend;

    [HttpPost]
    [Authorize(Roles = "Doctor")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public Task<IActionResult> Create([FromBody] CreateLabOrderRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, "api/lab-orders", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpGet("{labOrderId:guid}")]
    public Task<IActionResult> Get(Guid labOrderId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/lab-orders/{labOrderId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpGet("patients/{patientId:guid}")]
    public Task<IActionResult> GetByPatient(Guid patientId, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardAsync("prescription", HttpMethod.Get, $"api/lab-orders/patients/{patientId}", ForwardContext, cancellationToken: cancellationToken), cancellationToken);

    [HttpPost("{labOrderId:guid}/start")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Start(Guid labOrderId, [FromBody] StartLabOrderRequestDto? request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, $"api/lab-orders/{labOrderId}/start", ForwardContext, request ?? new StartLabOrderRequestDto(), cancellationToken), cancellationToken);

    [HttpPost("{labOrderId:guid}/cancel")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> Cancel(Guid labOrderId, [FromBody] CancelLabOrderRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, $"api/lab-orders/{labOrderId}/cancel", ForwardContext, request, cancellationToken), cancellationToken);

    [HttpPost("{labOrderId:guid}/results")]
    [Authorize(Roles = "Doctor")]
    public Task<IActionResult> RecordResults(Guid labOrderId, [FromBody] RecordLabOrderResultsRequestDto request, CancellationToken cancellationToken) =>
        Forward(_backend.ForwardJsonAsync("prescription", HttpMethod.Post, $"api/lab-orders/{labOrderId}/results", ForwardContext, request, cancellationToken), cancellationToken);
}
