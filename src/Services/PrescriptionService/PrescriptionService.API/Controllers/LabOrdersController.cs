using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PrescriptionService.API.Infrastructure;
using PrescriptionService.Application.DTOs;
using PrescriptionService.Application.Interfaces;

namespace PrescriptionService.API.Controllers;

[ApiController]
[Route("api/lab-orders")]
[Authorize]
public sealed class LabOrdersController : ControllerBase
{
    private readonly ILabOrderService _labOrders;

    public LabOrdersController(ILabOrderService labOrders) => _labOrders = labOrders;

    /// <summary>Создать направление на анализы (статус Ordered).</summary>
    [HttpPost]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<LabOrderResponse>> Create(
        [FromBody] CreateLabOrderRequest request,
        CancellationToken cancellationToken)
    {
        var doctorId = UserClaims.GetUserId(User);
        return Ok(await _labOrders.CreateAsync(doctorId, request, cancellationToken));
    }

    [HttpGet("{labOrderId:guid}")]
    public async Task<ActionResult<LabOrderResponse>> Get(Guid labOrderId, CancellationToken cancellationToken) =>
        Ok(await _labOrders.GetAsync(labOrderId, cancellationToken));

    /// <summary>Все направления пациента (для пациента и врача с доступом).</summary>
    [HttpGet("patients/{patientId:guid}")]
    public async Task<ActionResult<IReadOnlyList<LabOrderResponse>>> GetByPatient(
        Guid patientId,
        CancellationToken cancellationToken) =>
        Ok(await _labOrders.GetPatientOrdersAsync(patientId, cancellationToken));

    /// <summary>Лаборатория / врач взяли заказ в работу.</summary>
    [HttpPost("{labOrderId:guid}/start")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<LabOrderResponse>> Start(
        Guid labOrderId,
        [FromBody] StartLabOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _labOrders.StartAsync(labOrderId, userId, request ?? new StartLabOrderRequest(), cancellationToken));
    }

    [HttpPost("{labOrderId:guid}/cancel")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<LabOrderResponse>> Cancel(
        Guid labOrderId,
        [FromBody] CancelLabOrderRequest request,
        CancellationToken cancellationToken)
    {
        var userId = UserClaims.GetUserId(User);
        return Ok(await _labOrders.CancelAsync(labOrderId, userId, request.Reason, cancellationToken));
    }

    /// <summary>Записать результаты анализов (врач или лаборант через JWT).</summary>
    [HttpPost("{labOrderId:guid}/results")]
    [Authorize(Roles = "Doctor")]
    public async Task<ActionResult<LabOrderResponse>> RecordResults(
        Guid labOrderId,
        [FromBody] RecordLabOrderResultsRequest request,
        CancellationToken cancellationToken) =>
        Ok(await _labOrders.RecordResultsAsync(labOrderId, request, cancellationToken));
}
