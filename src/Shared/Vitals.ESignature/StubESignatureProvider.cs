namespace Vitals.ESignature;

public sealed class StubESignatureProvider : IESignatureProvider
{
    public Task<SignDocumentResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default)
    {
        var payload = $"{request.DocumentType}|{request.DocumentId}|{request.SignerId}|{request.ContentBase64[..Math.Min(64, request.ContentBase64.Length)]}";
        var signature = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"STUB-SIG:{payload}"));
        return Task.FromResult(new SignDocumentResult
        {
            Signature = signature,
            CertificateSerial = "STUB-CERT",
            SignedAt = DateTime.UtcNow
        });
    }
}
