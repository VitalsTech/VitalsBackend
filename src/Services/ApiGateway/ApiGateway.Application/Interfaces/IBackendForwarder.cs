using ApiGateway.Application.DTOs.Common;

namespace ApiGateway.Application.Interfaces;

public interface IBackendForwarder
{
    Task<HttpResponseMessage> ForwardJsonAsync(
        string serviceName,
        HttpMethod method,
        string relativePath,
        BackendForwardContext context,
        object? body = null,
        CancellationToken cancellationToken = default);

    Task<HttpResponseMessage> ForwardAsync(
        string serviceName,
        HttpMethod method,
        string relativePath,
        BackendForwardContext context,
        HttpContent? content = null,
        CancellationToken cancellationToken = default);
}
