using System.Net.Http.Headers;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Esia;

public sealed class EsiaOAuthService : IEsiaOAuthService
{
    private readonly EsiaOptions _options;
    private readonly HttpClient _http;

    public EsiaOAuthService(IOptions<EsiaOptions> options, HttpClient http)
    {
        _options = options.Value;
        _http = http;
    }

    public string BuildAuthorizationUrl(string state)
    {
        if (!_options.Enabled)
            throw new EsiaNotConfiguredException();

        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = _options.Scope,
            ["state"] = state,
            ["access_type"] = "offline",
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()
        };

        var qs = string.Join("&", query.Select(kv =>
            $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(kv.Value)}"));

        return $"{_options.AuthorizationEndpoint}?{qs}";
    }

    public async Task<EsiaUserInfo> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
            throw new EsiaNotConfiguredException();

        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["redirect_uri"] = _options.RedirectUri
            })
        };

        var tokenResponse = await _http.SendAsync(tokenRequest, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();

        await using var tokenStream = await tokenResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var tokenDoc = await JsonDocument.ParseAsync(tokenStream, cancellationToken: cancellationToken);
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString()
            ?? throw new AuthValidationException("ESIA token response does not contain access_token.");

        if (string.IsNullOrWhiteSpace(_options.UserInfoEndpoint))
            throw new AuthValidationException("Esia:UserInfoEndpoint is not configured.");

        using var userInfoRequest = new HttpRequestMessage(HttpMethod.Get, _options.UserInfoEndpoint);
        userInfoRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userInfoResponse = await _http.SendAsync(userInfoRequest, cancellationToken);
        userInfoResponse.EnsureSuccessStatusCode();

        await using var userStream = await userInfoResponse.Content.ReadAsStreamAsync(cancellationToken);
        using var userDoc = await JsonDocument.ParseAsync(userStream, cancellationToken: cancellationToken);
        var root = userDoc.RootElement;

        return new EsiaUserInfo
        {
            SubjectId = GetString(root, "sub", "oid") ?? throw new AuthValidationException("ESIA user info does not contain subject id."),
            Snils = GetString(root, "snils"),
            Phone = GetString(root, "mobile", "phone"),
            Email = GetString(root, "email"),
            FirstName = GetString(root, "firstName", "given_name"),
            LastName = GetString(root, "lastName", "family_name"),
            MiddleName = GetString(root, "middleName", "middle_name")
        };
    }

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String)
                return value.GetString();
        }

        return null;
    }
}
