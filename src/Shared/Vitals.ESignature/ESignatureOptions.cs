namespace Vitals.ESignature;

public sealed class ESignatureOptions
{
    public const string SectionName = "ESignature";

    public bool UseStub { get; set; } = true;
    public string Provider { get; set; } = "Http";
    public string? SignEndpoint { get; set; }
    public string? ApiKey { get; set; }
    public string? CertificateThumbprint { get; set; }
    public string? TspUrl { get; set; }
}
