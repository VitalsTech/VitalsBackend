using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AuthService.Application.DTOs;
using AuthService.Application.Exceptions;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.Clients;

public sealed class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient http, IOptions<UserServiceOptions> options, ILogger<UserServiceClient> logger)
    {
        _http = http;
        _logger = logger;
        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(options.Value.BaseUrl.TrimEnd('/') + "/");
    }

    public async Task<UserServiceUserDto> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync("api/users/register", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Conflict)
            throw new DuplicatePhoneException(request.PhoneNumber);

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<UserWithProfilesResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            throw new AuthValidationException("UserService returned an empty registration response.");

        return new UserServiceUserDto
        {
            PublicId = payload.PublicId,
            PhoneNumber = payload.PhoneNumber,
            IsActive = payload.IsActive
        };
    }

    public async Task<UserServiceUserDto?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken = default)
    {
        var encoded = Uri.EscapeDataString(phone);
        var response = await _http.GetAsync($"internal/users/by-phone/{encoded}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<UserWithProfilesResponse>(cancellationToken: cancellationToken);
        if (payload is null)
            return null;

        return new UserServiceUserDto
        {
            PublicId = payload.PublicId,
            PhoneNumber = payload.PhoneNumber,
            IsActive = payload.IsActive
        };
    }

    public async Task<UserServiceRoleResponse> GetRolesAndPermissionsAsync(
        Guid userPublicId,
        CancellationToken cancellationToken = default)
    {
        var response = await _http.GetAsync(
            $"internal/users/{userPublicId}/roles-permissions",
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return new UserServiceRoleResponse { UserPublicId = userPublicId };

        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserServiceRoleResponse>(cancellationToken: cancellationToken)
            ?? new UserServiceRoleResponse { UserPublicId = userPublicId };
    }

    public async Task SwitchActiveProfileAsync(Guid userPublicId, Guid profileId, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"internal/users/{userPublicId}/switch-profile",
            new { userPublicId, profileId },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ActivateProfileByTypeAsync(Guid userPublicId, string profileType, CancellationToken cancellationToken = default)
    {
        var response = await _http.PostAsJsonAsync(
            $"internal/users/{userPublicId}/switch-profile",
            new { userPublicId, profileType },
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task ApplyEsiaProfileAsync(Guid userPublicId, EsiaUserInfo esiaUser, CancellationToken cancellationToken = default)
    {
        var body = new
        {
            firstName = esiaUser.FirstName,
            secondName = esiaUser.MiddleName,
            surename = esiaUser.LastName,
            birthDate = esiaUser.BirthDate,
            sex = MapSex(esiaUser.Gender),
            email = esiaUser.Email,
            snils = NormalizeSnils(esiaUser.Snils),
            insuranceNumber = esiaUser.OmsNumber,
            residenceAddress = MapAddress(esiaUser.ResidenceAddress),
            registrationAddress = MapAddress(esiaUser.RegistrationAddress)
        };

        var response = await _http.PostAsJsonAsync(
            $"internal/users/{userPublicId}/esia-profile",
            body,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static string? NormalizeSnils(string? snils)
    {
        if (string.IsNullOrWhiteSpace(snils))
            return null;
        var digits = new string(snils.Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? digits : null;
    }

    private static string? MapSex(string? gender) => gender?.Trim().ToUpperInvariant() switch
    {
        "M" or "MALE" or "МУЖ" or "МУЖСКОЙ" => "Male",
        "F" or "FEMALE" or "ЖЕН" or "ЖЕНСКИЙ" => "Female",
        _ => null
    };

    private static object? MapAddress(EsiaAddressInfo? address)
    {
        if (address is null)
            return null;
        return new
        {
            postCode = address.PostCode,
            country = address.Country,
            region = address.Region,
            city = address.City,
            area = address.Area,
            street = address.Street ?? address.AddressStr,
            house = address.House,
            flat = address.Flat
        };
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("UserService call failed: {Status} {Body}", response.StatusCode, body);
        throw new AuthValidationException($"UserService error: {(int)response.StatusCode}");
    }

    private sealed class UserWithProfilesResponse
    {
        public Guid PublicId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }
}
