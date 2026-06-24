namespace Vitals.AspNetCore.Authentication;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? JwksUrl { get; set; }
    public string? RsaPublicKeyPem { get; set; }
    public bool AllowInsecureDevelopmentBypass { get; set; }
    public bool AllowDevelopmentHeaderFallback { get; set; }
}

public sealed class ServiceAuthOptions
{
    public const string SectionName = "ServiceAuth";
    public string ApiKey { get; set; } = string.Empty;
}
