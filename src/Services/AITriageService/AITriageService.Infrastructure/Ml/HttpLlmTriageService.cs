using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AITriageService.Application.DTOs;
using AITriageService.Application.Interfaces;
using AITriageService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AITriageService.Infrastructure.Ml;

public sealed class HttpLlmTriageService : ILlmTriageService
{
    private readonly HttpClient _http;
    private readonly MlServicesOptions _options;
    private readonly ILogger<HttpLlmTriageService> _logger;
    private readonly StubLlmTriageService _fallback;

    public HttpLlmTriageService(
        HttpClient http,
        IOptions<MlServicesOptions> options,
        ILogger<HttpLlmTriageService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
        _fallback = new StubLlmTriageService();
    }

    public async Task<LlmTriageResultDto> AnalyzeAsync(TriagePromptContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.LlmEndpoint))
            return await _fallback.AnalyzeAsync(context, cancellationToken).ConfigureAwait(false);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.LlmEndpoint)
            {
                Content = new StringContent(JsonSerializer.Serialize(context), Encoding.UTF8, "application/json")
            };

            if (!string.IsNullOrWhiteSpace(_options.ApiKey))
                request.Headers.Add("Authorization", $"Bearer {_options.ApiKey}");

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<LlmTriageResultDto>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
                return await _fallback.AnalyzeAsync(context, cancellationToken).ConfigureAwait(false);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "LLM endpoint {Endpoint} failed; using heuristic fallback", _options.LlmEndpoint);
            return await _fallback.AnalyzeAsync(context, cancellationToken).ConfigureAwait(false);
        }
    }
}
