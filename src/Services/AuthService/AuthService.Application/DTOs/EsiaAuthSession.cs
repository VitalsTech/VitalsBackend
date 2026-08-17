namespace AuthService.Application.DTOs;

public sealed class EsiaAuthSession
{
    public string State { get; set; } = string.Empty;
    public string Intent { get; set; } = "register";
    public Guid? UserPublicId { get; set; }
    public string? ReturnUrl { get; set; }
    public string ResponseMode { get; set; } = "fragment";
    public string? DeviceFingerprint { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
