namespace AuthService.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public int AccessTokenMinutes { get; set; } = 15;
    public int RefreshTokenDays { get; set; } = 30;
    public string? RsaPrivateKeyPem { get; set; }
    public string? RsaPublicKeyPem { get; set; }
    public string KeyId { get; set; } = "vitals-auth-key-1";
}
