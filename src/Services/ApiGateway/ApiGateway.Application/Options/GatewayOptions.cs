namespace ApiGateway.Application.Options;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? RsaPublicKeyPem { get; set; }
    public string? JwksUrl { get; set; }
}

public sealed class BackendServicesOptions
{
    public const string SectionName = "Services";

    public string AuthService { get; set; } = "http://localhost:5200";
    public string UserService { get; set; } = "http://localhost:5195";
    public string MedicalRecordService { get; set; } = "http://localhost:5210";
}

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public string RedisConnectionString { get; set; } = "localhost:6379";
    public Dictionary<string, RateLimitPolicyOptions> Policies { get; set; } = new();
    public List<EndpointRateLimitRule> EndpointRules { get; set; } = [];
}

public sealed class RateLimitPolicyOptions
{
    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public RateLimitPartitionKind PartitionBy { get; set; } = RateLimitPartitionKind.Ip;
}

public enum RateLimitPartitionKind
{
    Ip,
    UserId
}

public sealed class EndpointRateLimitRule
{
    public string Path { get; set; } = string.Empty;
    public string[] Methods { get; set; } = ["POST"];
    public string Policy { get; set; } = string.Empty;
}
