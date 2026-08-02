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

    /// <summary>Создать черновик (и подписать при confirmWarnings=true) из Consultation complete.</summary>
    [HttpPost]
    public async Task<ActionResult<PrescriptionResponse>> Create(
        [FromBody] InternalCreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        if (request.DoctorId == Guid.Empty)
            return BadRequest(new { error = "doctorId обязателен." });

        return Ok(await _prescriptions.CreateDraftAsync(request.DoctorId, request, cancellationToken));
    }

    [HttpPost("{prescriptionId:guid}/fulfillment")]
    public async Task<ActionResult<PrescriptionResponse>> Fulfillment(
        Guid prescriptionId,
        [FromBody] FulfillmentRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _prescriptions.RecordFulfillmentAsync(prescriptionId, request, cancellationToken));
}
