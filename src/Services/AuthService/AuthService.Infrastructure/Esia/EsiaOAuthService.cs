using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Esia;

public sealed class EsiaOAuthService : IEsiaOAuthService
{
    private static readonly HashSet<string> MedicalDocTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MDCL_PLCY", "MDCL_POLICY", "OMS", "DMS", "MDCL_CERT", "MEDICAL_POLICY"
    };

    private readonly EsiaOptions _options;
    private readonly HttpClient _http;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<EsiaOAuthService> _logger;

    public EsiaOAuthService(
        IOptions<EsiaOptions> options,
        HttpClient http,
        IHostEnvironment environment,
        ILogger<EsiaOAuthService> logger)
    {
        _options = options.Value;
        _http = http;
        _environment = environment;
        _logger = logger;
        _http.Timeout = TimeSpan.FromSeconds(30);
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/json");
    }

    public bool IsEnabledOnThisEnvironment =>
        (!_environment.IsProduction() || _options.AllowProduction) && _options.Enabled;

    public bool UseStub => IsEnabledOnThisEnvironment && _options.UseStub;

    public bool IsAvailable =>
        UseStub
        || (IsEnabledOnThisEnvironment
            && !string.IsNullOrWhiteSpace(_options.ClientId)
            && !string.IsNullOrWhiteSpace(_options.CertificatePath)
            && File.Exists(_options.CertificatePath));

    public EsiaConfigResponse GetPublicConfig() => new()
    {
        Enabled = IsEnabledOnThisEnvironment,
        Mode = UseStub ? "stub" : "oauth",
        Configured = IsAvailable,
        Portal = UseStub ? "stub" : _options.RestBaseUrl.TrimEnd('/'),
        RedirectUri = _options.RedirectUri,
        Collects = UseStub ? ["fullName", "email", "phone"] : [],
        Generates = UseStub
            ? ["oms", "snils", "birthDate", "gender", "residenceAddress", "registrationAddress", "medicalRecordSnapshot"]
            : ["fullName", "oms", "residenceAddress", "medicalRecordSnapshot"]
    };

    public bool IsAllowedReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return false;

        if (!Uri.TryCreate(returnUrl, UriKind.Absolute, out _))
            return false;

        return _options.AllowedReturnUrlPrefixes.Any(prefix =>
            returnUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }

    public string BuildAuthorizationUrl(string state)
    {
        EnsureAvailable();
        var signer = new EsiaRequestSigner(_options);
        if (!signer.CanSign)
            throw new AuthValidationException(EsiaRequestSigner.MissingCertMessage);

        var timestamp = EsiaRequestSigner.CreateTimestamp();
        var query = new Dictionary<string, string>
        {
            ["client_id"] = _options.ClientId,
            ["client_secret"] = signer.SignAuthorization(timestamp, state),
            ["redirect_uri"] = _options.RedirectUri,
            ["response_type"] = "code",
            ["scope"] = _options.Scope,
            ["state"] = state,
            ["timestamp"] = timestamp,
            ["access_type"] = "online",
            ["client_certificate_hash"] = signer.GetCertificateHash()
        };

        return $"{_options.AuthorizationEndpoint}?{EsiaRequestSigner.ToQueryString(query)}";
    }

    public async Task<EsiaUserInfo> ExchangeCodeAsync(string code, string? state = null, CancellationToken cancellationToken = default)
    {
        EnsureAvailable();
        var signer = new EsiaRequestSigner(_options);
        if (!signer.CanSign)
            throw new AuthValidationException(EsiaRequestSigner.MissingCertMessage);

        var timestamp = EsiaRequestSigner.CreateTimestamp();
        var tokenState = string.IsNullOrWhiteSpace(state) ? Guid.NewGuid().ToString("N") : state;
        var tokenForm = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["client_id"] = _options.ClientId,
            ["client_secret"] = signer.SignToken(timestamp, tokenState, code),
            ["redirect_uri"] = _options.RedirectUri,
            ["scope"] = _options.Scope,
            ["timestamp"] = timestamp,
            ["token_type"] = "Bearer",
            ["state"] = tokenState,
            ["client_certificate_hash"] = signer.GetCertificateHash()
        };
        using var tokenRequest = new HttpRequestMessage(HttpMethod.Post, _options.TokenEndpoint)
        {
            Content = new StringContent(
                EsiaRequestSigner.ToQueryString(tokenForm),
                Encoding.UTF8,
                "application/x-www-form-urlencoded")
        };

        var tokenResponse = await _http.SendAsync(tokenRequest, cancellationToken);
        var tokenBody = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            _logger.LogWarning("ESIA token exchange failed: {Status} {Body}", tokenResponse.StatusCode, tokenBody);
            throw new AuthValidationException(
                "Не удалось обменять код Госуслуг на токен. Проверьте сертификат ИС, client_certificate_hash и RedirectUri.");
        }

        using var tokenDoc = JsonDocument.Parse(tokenBody);
        var accessToken = tokenDoc.RootElement.TryGetProperty("access_token", out var tokenEl)
            ? tokenEl.GetString()
            : null;
        if (string.IsNullOrWhiteSpace(accessToken))
            throw new AuthValidationException("ESIA token response does not contain access_token.");

        var oid = ReadSubjectId(accessToken);
        var person = await GetJsonAsync($"rs/prns/{oid}?embed=(contacts.elements,addresses.elements,documents.elements)", accessToken, cancellationToken)
                     ?? await GetJsonAsync($"rs/prns/{oid}", accessToken, cancellationToken)
                     ?? throw new AuthValidationException("ЕСИА не вернула профиль пользователя.");

        var contacts = await TryGetElementsAsync($"rs/prns/{oid}/ctts?embed=(elements)", accessToken, person, "contacts", cancellationToken);
        var addresses = await TryGetElementsAsync($"rs/prns/{oid}/addrs?embed=(elements)", accessToken, person, "addresses", cancellationToken);
        var documents = await TryGetElementsAsync($"rs/prns/{oid}/docs?embed=(elements)", accessToken, person, "documents", cancellationToken);

        var parsedDocs = documents.Select(ParseDocument).Where(d => d.Type is not null).ToList();
        var oms = parsedDocs.FirstOrDefault(d =>
            MedicalDocTypes.Contains(d.Type ?? "") ||
            string.Equals(d.Type, "MDCL_PLCY", StringComparison.OrdinalIgnoreCase));

        var residence = addresses
            .Select(ParseAddress)
            .FirstOrDefault(a => string.Equals(a.Type, "PLV", StringComparison.OrdinalIgnoreCase))
            ?? addresses.Select(ParseAddress).FirstOrDefault();

        var registration = addresses
            .Select(ParseAddress)
            .FirstOrDefault(a => string.Equals(a.Type, "PRG", StringComparison.OrdinalIgnoreCase));

        return new EsiaUserInfo
        {
            SubjectId = oid,
            Snils = DigitsOnly(GetString(person, "snils")),
            OmsNumber = DigitsOnly(oms?.Number),
            OmsSeries = oms?.Series,
            Phone = FindContact(contacts, "MBT", "mobile", "phone") ?? GetString(person, "mobile", "phone"),
            Email = FindContact(contacts, "EML", "email") ?? GetString(person, "email"),
            FirstName = GetString(person, "firstName", "given_name"),
            LastName = GetString(person, "lastName", "family_name"),
            MiddleName = GetString(person, "middleName", "middle_name"),
            BirthDate = ParseDate(GetString(person, "birthDate", "birthdate")),
            Gender = GetString(person, "gender", "sex"),
            ResidenceAddress = residence,
            RegistrationAddress = registration,
            MedicalDocuments = parsedDocs
                .Where(d => MedicalDocTypes.Contains(d.Type ?? ""))
                .ToList()
        };
    }

    private void EnsureAvailable()
    {
        if (_environment.IsProduction() && !_options.AllowProduction)
            throw new EsiaNotConfiguredException();
        if (!_options.Enabled)
            throw new EsiaNotConfiguredException();
        if (!UseStub && string.IsNullOrWhiteSpace(_options.ClientId))
            throw new AuthValidationException("ESIA ClientId is not configured. Set ESIA_CLIENT_ID for DEV.");
        if (!UseStub && string.IsNullOrWhiteSpace(_options.CertificatePath))
            throw new AuthValidationException(EsiaRequestSigner.MissingCertMessage);
    }

    private static string ReadSubjectId(string accessToken)
    {
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
        var oid = jwt.Claims.FirstOrDefault(c => c.Type is "urn:esia:sbj_id" or "sub" or "oid")?.Value;
        if (string.IsNullOrWhiteSpace(oid))
            throw new AuthValidationException("ESIA access_token does not contain subject id.");
        return oid;
    }

    private async Task<JsonElement?> GetJsonAsync(string relativePath, string accessToken, CancellationToken cancellationToken)
    {
        var url = $"{_options.RestBaseUrl.TrimEnd('/')}/{relativePath.TrimStart('/')}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await _http.SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogInformation("ESIA GET {Path} -> {Status}", relativePath, response.StatusCode);
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.Clone();
        }
        catch (JsonException)
        {
            _logger.LogWarning("ESIA GET {Path} returned non-JSON", relativePath);
            return null;
        }
    }

    private async Task<List<JsonElement>> TryGetElementsAsync(
        string relativePath,
        string accessToken,
        JsonElement person,
        string embedName,
        CancellationToken cancellationToken)
    {
        if (person.TryGetProperty(embedName, out var embedded))
        {
            var fromEmbed = ReadElements(embedded);
            if (fromEmbed.Count > 0)
                return fromEmbed;
        }

        var resource = await GetJsonAsync(relativePath, accessToken, cancellationToken);
        return resource is null ? [] : ReadElements(resource.Value);
    }

    private static List<JsonElement> ReadElements(JsonElement root)
    {
        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("elements", out var elements) &&
            elements.ValueKind == JsonValueKind.Array)
        {
            return elements.EnumerateArray().Select(e => e.Clone()).ToList();
        }

        if (root.ValueKind == JsonValueKind.Array)
            return root.EnumerateArray().Select(e => e.Clone()).ToList();

        return [];
    }

    private static EsiaAddressInfo ParseAddress(JsonElement element) => new()
    {
        Type = GetString(element, "type"),
        PostCode = GetString(element, "zipCode", "postalCode"),
        Country = GetString(element, "countryId", "country"),
        Region = GetString(element, "region"),
        City = GetString(element, "city"),
        Area = GetString(element, "area", "district"),
        Street = GetString(element, "street") ?? GetString(element, "addressStr"),
        House = GetString(element, "house"),
        Flat = GetString(element, "flat", "apartment"),
        AddressStr = GetString(element, "addressStr")
    };

    private static EsiaDocumentInfo ParseDocument(JsonElement element) => new()
    {
        Type = GetString(element, "type", "docType"),
        Series = GetString(element, "series"),
        Number = GetString(element, "number"),
        IssueDate = GetString(element, "issueDate", "issuedOn"),
        IssuedBy = GetString(element, "issuedBy", "issuer")
    };

    private static string? FindContact(IEnumerable<JsonElement> contacts, params string[] types)
    {
        foreach (var contact in contacts)
        {
            var type = GetString(contact, "type");
            if (type is null)
                continue;
            if (types.Any(t => t.Equals(type, StringComparison.OrdinalIgnoreCase)))
                return GetString(contact, "value");
        }

        return null;
    }

    private static string? GetString(JsonElement root, params string[] names)
    {
        foreach (var name in names)
        {
            if (!root.TryGetProperty(name, out var value))
                continue;
            if (value.ValueKind == JsonValueKind.String)
                return value.GetString();
            if (value.ValueKind is JsonValueKind.Number)
                return value.ToString();
        }

        return null;
    }

    private static string? DigitsOnly(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        var digits = new string(value.Where(char.IsDigit).ToArray());
        return digits.Length == 0 ? null : digits;
    }

    private static DateTime? ParseDate(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;
        var formats = new[] { "dd.MM.yyyy", "yyyy-MM-dd", "dd.MM.yyyy HH:mm:ss" };
        if (DateTime.TryParseExact(raw, formats, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AssumeUniversal, out var parsed))
            return DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);
        if (DateTime.TryParse(raw, out var fallback))
            return DateTime.SpecifyKind(fallback.Date, DateTimeKind.Utc);
        return null;
    }
}
