using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace PrescriptionService.API.Controllers;

[ApiController]
[Route("internal/prescriptions")]
public sealed class InternalPrescriptionController : ControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public InternalPrescriptionController(IPrescriptionService prescriptions) => _prescriptions = prescriptions;

    [HttpPost("{prescriptionId:guid}/fulfillment")]
    public async Task<ActionResult<PrescriptionResponse>> Fulfillment(
        Guid prescriptionId,
        [FromBody] FulfillmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _prescriptions.RecordFulfillmentAsync(prescriptionId, request, cancellationToken));
}
