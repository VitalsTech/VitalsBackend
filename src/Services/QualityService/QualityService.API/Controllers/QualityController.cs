using QualityService.Application.DTOs;
using QualityService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace QualityService.API.Controllers;

[ApiController]
[Route("api/quality")]
[Authorize]
public sealed class QualityController : ControllerBase
{
    private readonly IQualityService _quality;

    public QualityController(IQualityService quality) => _quality = quality;

    [HttpGet("metrics")]
    public Task<QualityMetricsResponse> Metrics(CancellationToken cancellationToken) =>
        _quality.GetMetricsAsync(cancellationToken);
}
