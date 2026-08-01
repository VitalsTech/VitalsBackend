using System.Security.Claims;
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

    protected Guid? CurrentUserId
    {
        get
        {
            var sub = User.FindFirst("sub")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    /// <summary>Sub + все profile_id из токена: сервисы хранят разные идентификаторы одного и того же пользователя.</summary>
    protected IReadOnlyList<Guid> CurrentIdentityIds
    {
        get
        {
            var ids = new List<Guid>();
            if (CurrentUserId is { } userId)
                ids.Add(userId);

            foreach (var claim in User.FindAll("profile_id"))
            {
                if (Guid.TryParse(claim.Value, out var profileId) && profileId != Guid.Empty && !ids.Contains(profileId))
                    ids.Add(profileId);
            }

            return ids;
        }
    }

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
