using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Vitals.ESignature;

/// <summary>
/// HTTP adapter for CryptoPro DSS / similar КЭП REST gateway.
/// </summary>
public sealed class HttpESignatureProvider : IESignatureProvider
{
    private readonly HttpClient _http;
    private readonly ESignatureOptions _options;
    private readonly ILogger<HttpESignatureProvider> _logger;

    public HttpESignatureProvider(
        HttpClient http,
        IOptions<ESignatureOptions> options,
        ILogger<HttpESignatureProvider> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<SignDocumentResult> SignAsync(SignDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SignEndpoint))
            throw new InvalidOperationException("ESignature:SignEndpoint is required when UseStub=false.");

        var body = new
        {
            documentType = request.DocumentType,
            documentId = request.DocumentId,
            signerId = request.SignerId,
            contentBase64 = request.ContentBase64,
            certificateThumbprint = request.CertificateThumbprint ?? _options.CertificateThumbprint,
            tspUrl = _options.TspUrl
        };

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, _options.SignEndpoint)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            httpRequest.Headers.Add("X-Api-Key", _options.ApiKey);

        var response = await _http.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogError("ESignature provider returned {Status}: {Error}", response.StatusCode, error);
            response.EnsureSuccessStatusCode();
        }

        var payload = await response.Content.ReadFromJsonAsync<ProviderSignResponse>(cancellationToken: cancellationToken)
            .ConfigureAwait(false)
            ?? throw new InvalidOperationException("ESignature provider returned empty response.");

        return new SignDocumentResult
        {
            Signature = payload.Signature ?? payload.CmsSignature ?? throw new InvalidOperationException("Signature missing in provider response."),
            CertificateSerial = payload.CertificateSerial,
            SignedAt = payload.SignedAt ?? DateTime.UtcNow
        };
    }

    private sealed class ProviderSignResponse
    {
        public string? Signature { get; set; }
        public string? CmsSignature { get; set; }
        public string? CertificateSerial { get; set; }
        public DateTime? SignedAt { get; set; }
    }
}
