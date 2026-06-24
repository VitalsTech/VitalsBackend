using ApiGateway.Application.DTOs.Common;
using ApiGateway.API.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ApiGateway.API.Controllers.V1;

[ApiController]
public abstract class GatewayControllerBase : ControllerBase
{
    protected BackendForwardContext ForwardContext => new()
    {
        Authorization = Request.Headers.Authorization.ToString(),
        RequestId = HttpContext.TraceIdentifier,
        DeviceFingerprint = Request.Headers["X-Device-Fingerprint"].FirstOrDefault(),
        ClientIp = HttpContext.Connection.RemoteIpAddress?.ToString()
    };

    protected async Task<IActionResult> ForwardResponse(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        await HttpResponseForwarder.ForwardAsync(HttpContext, response, cancellationToken);
        return new EmptyResult();
    }

    protected async Task<IActionResult> Forward(Task<HttpResponseMessage> responseTask, CancellationToken cancellationToken)
    {
        using var response = await responseTask.ConfigureAwait(false);
        return await ForwardResponse(response, cancellationToken);
    }
}
