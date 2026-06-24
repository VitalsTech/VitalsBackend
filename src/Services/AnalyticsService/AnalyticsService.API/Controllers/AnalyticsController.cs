using AnalyticsService.Application.DTOs;
using AnalyticsService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public AnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpGet("dashboard")]
    public Task<AnalyticsDashboardResponse> Dashboard(CancellationToken cancellationToken) =>
        _analytics.GetDashboardAsync(cancellationToken);
}

[ApiController]
[Route("internal/analytics")]
[Authorize(Policy = "InternalService")]
public sealed class InternalAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analytics;

    public InternalAnalyticsController(IAnalyticsService analytics) => _analytics = analytics;

    [HttpPost("metrics")]
    public async Task<IActionResult> RecordMetric([FromBody] RecordMetricRequest request, CancellationToken cancellationToken)
    {
        await _analytics.RecordMetricAsync(request, cancellationToken);
        return Accepted();
    }
}
