using System.Net.Http.Json;
using MedicalRecordService.Application.Interfaces;
using MedicalRecordService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MedicalRecordService.Infrastructure.Clients;

public sealed class UserIdentityClient : IUserIdentityClient
{
    private readonly HttpClient _http;
    private readonly ILogger<UserIdentityClient> _logger;

    public UserIdentityClient(
        HttpClient http,
        IOptions<UserServiceOptions> options,
        ILogger<UserIdentityClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<IReadOnlyList<Guid>> ResolveIdentityIdsAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"internal/users/{id}/identity-ids");
            request.Headers.TryAddWithoutValidation("X-Service-Name", "medical-record");

            var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Identity resolve failed for {Id}: {Status}",
                    id,
                    response.StatusCode);
                return new[] { id };
            }

            var body = await response.Content
                .ReadFromJsonAsync<IdentityIdsResponse>(cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var ids = (body?.IdentityIds ?? Enumerable.Empty<Guid>())
                .Where(x => x != Guid.Empty)
                .Distinct()
                .ToList();
            if (ids.Count == 0)
                ids.Add(id);
            else if (!ids.Contains(id))
                ids.Add(id);
            return ids;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Identity resolve error for {Id}", id);
            return new[] { id };
        }
    }

    private sealed class IdentityIdsResponse
    {
        public List<Guid>? IdentityIds { get; set; }
    }
}
