using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Ml;

public sealed class HttpNerService : INerService
{
    private readonly HttpClient _http;
    private readonly MlServicesOptions _options;
    private readonly ILogger<HttpNerService> _logger;
    private readonly StubNerService _fallback;

    public HttpNerService(
        HttpClient http,
        IOptions<MlServicesOptions> options,
        ILogger<HttpNerService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _fallback = new StubNerService();
    }

    public async Task<IReadOnlyList<NerEntityDto>> ExtractAsync(
        string text,
        IReadOnlyList<ExtractedEntityDto> parsed,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.NerEndpoint))
            return await _fallback.ExtractAsync(text, parsed, cancellationToken).ConfigureAwait(false);

        try
        {
            using var request = CreateRequest(_options.NerEndpoint, new { text, entities = parsed });
            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<NerApiResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (payload?.Entities is null || payload.Entities.Count == 0)
                return await _fallback.ExtractAsync(text, parsed, cancellationToken).ConfigureAwait(false);

            return payload.Entities;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "NER endpoint {Endpoint} failed; using rule-based fallback", _options.NerEndpoint);
            return await _fallback.ExtractAsync(text, parsed, cancellationToken).ConfigureAwait(false);
        }
    }

    private HttpRequestMessage CreateRequest(string url, object body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };

        if (!string.IsNullOrWhiteSpace(_options.ApiKey))
            request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

        return request;
    }

    private sealed class NerApiResponse
    {
        public List<NerEntityDto>? Entities { get; set; }
    }
}
