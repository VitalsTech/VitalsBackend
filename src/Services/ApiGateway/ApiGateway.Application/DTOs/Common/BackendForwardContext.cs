namespace ApiGateway.Application.DTOs.Common;

public sealed class BackendForwardContext
{
    public string? Authorization { get; init; }
    public string? RequestId { get; init; }
    public string? DeviceFingerprint { get; init; }
    public string? ClientIp { get; init; }
}
