namespace Vitals.ESignature;

public sealed class SignDocumentRequest
{
    public required string DocumentType { get; init; }
    public required Guid DocumentId { get; init; }
    public required Guid SignerId { get; init; }
    public required string ContentBase64 { get; init; }
    public string? CertificateThumbprint { get; init; }
}

public sealed class SignDocumentResult
{
    public required string Signature { get; init; }
    public string? CertificateSerial { get; init; }
    public DateTime? SignedAt { get; init; }
}

public interface IESignatureProvider
{
    Task<SignDocumentResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default);
}
