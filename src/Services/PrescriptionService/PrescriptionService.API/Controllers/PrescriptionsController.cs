using PrescriptionService.API.Infrastructure;
using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PrescriptionService.API.Controllers;

[ApiController]
[Route("api/prescriptions")]
[Authorize]
public sealed class PrescriptionsController : ControllerBase
{
    private readonly IPrescriptionService _prescriptions;

    public PrescriptionsController(IPrescriptionService prescriptions) => _prescriptions = prescriptions;

    [HttpPost]
    public async Task<ActionResult<PrescriptionResponse>> Create(
        [FromBody] CreatePrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var doctorId = UserClaims.GetUserId(User);
        return Ok(await _prescriptions.CreateDraftAsync(doctorId, request, cancellationToken));
    }

    [HttpGet("{prescriptionId:guid}")]
    public async Task<ActionResult<PrescriptionResponse>> Get(Guid prescriptionId, CancellationToken cancellationToken) =>
        Ok(await _prescriptions.GetAsync(prescriptionId, cancellationToken));

    [HttpGet("patients/{patientId:guid}")]
    public async Task<ActionResult<IReadOnlyList<PrescriptionResponse>>> GetPatientPrescriptions(
        Guid patientId,
        CancellationToken cancellationToken) =>
        Ok(await _prescriptions.GetPatientPrescriptionsAsync(patientId, cancellationToken));

    [HttpPost("{prescriptionId:guid}/validate")]
    public async Task<ActionResult<PrescriptionValidationResultDto>> Validate(
        Guid prescriptionId,
        [FromQuery] bool confirmWarnings = false,
        CancellationToken cancellationToken = default) =>
        Ok(await _prescriptions.ValidateAsync(prescriptionId, confirmWarnings, cancellationToken));

    [HttpPost("{prescriptionId:guid}/sign")]
    public async Task<ActionResult<PrescriptionResponse>> Sign(
        Guid prescriptionId,
        [FromQuery] bool confirmWarnings = false,
        CancellationToken cancellationToken = default)
    {
        var doctorId = UserClaims.GetUserId(User);
        return Ok(await _prescriptions.SignAsync(prescriptionId, doctorId, confirmWarnings, cancellationToken));
    }

    [HttpPost("{prescriptionId:guid}/send-to-pharmacy")]
    public async Task<ActionResult<PrescriptionResponse>> SendToPharmacy(
        Guid prescriptionId,
        [FromBody] SendToPharmacyRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _prescriptions.SendToPharmacyAsync(prescriptionId, userId, request, cancellationToken));
    }

    [HttpPost("{prescriptionId:guid}/cancel")]
    public async Task<ActionResult<PrescriptionResponse>> Cancel(
        Guid prescriptionId,
        [FromBody] CancelPrescriptionRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _prescriptions.CancelAsync(prescriptionId, userId, request.Reason, cancellationToken));
    }

    [HttpGet("{prescriptionId:guid}/qr")]
    public async Task<ActionResult<QrCodeResponse>> Qr(Guid prescriptionId, CancellationToken cancellationToken) =>
        Ok(await _prescriptions.GetQrCodeAsync(prescriptionId, cancellationToken));

    [HttpGet("{prescriptionId:guid}/instructions")]
    public async Task<ActionResult<IReadOnlyList<PatientInstructionDto>>> Instructions(
        Guid prescriptionId,
        CancellationToken cancellationToken) =>
        Ok(await _prescriptions.GetPatientInstructionsAsync(prescriptionId, cancellationToken));
}
