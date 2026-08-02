using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace PrescriptionService.API.Controllers;

[ApiController]
[Route("internal/lab-orders")]
public sealed class InternalLabOrderController : ControllerBase
{
    private readonly ILabOrderService _labOrders;

    public InternalLabOrderController(ILabOrderService labOrders) => _labOrders = labOrders;

    [HttpPost]
    public async Task<ActionResult<LabOrderResponse>> Create(
        [FromBody] InternalCreateLabOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DoctorId == Guid.Empty)
            return BadRequest(new { error = "doctorId обязателен." });

        return Ok(await _labOrders.CreateAsync(request.DoctorId, request, cancellationToken));
    }

    /// <summary>Результаты от внешней лаборатории / IntegrationService.</summary>
    [HttpPost("{labOrderId:guid}/results")]
    public async Task<ActionResult<LabOrderResponse>> RecordResults(
        Guid labOrderId,
        [FromBody] RecordLabOrderResultsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _labOrders.RecordResultsAsync(labOrderId, request, cancellationToken));
}
