using AuthService.Application.DTOs;
using AuthService.Application.Esia;
using AuthService.Application.Exceptions;
using AuthService.Application.Helpers;
using AuthService.Application.Interfaces;
using AuthService.Application.Options;
using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Application.Services;

public sealed class AuthenticationService : IAuthenticationService
{
    private readonly IAuthUserRepository _authUsers;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordResetTokenRepository _passwordResets;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IEsiaOAuthService _esiaOAuthService;
    private readonly IMedicalRecordEventClient _medicalRecord;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthenticationService> _logger;

    public AuthenticationService(
        IAuthUserRepository authUsers,
        IRefreshTokenRepository refreshTokens,
        IPasswordResetTokenRepository passwordResets,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IUserServiceClient userServiceClient,
        IEsiaOAuthService esiaOAuthService,
        IMedicalRecordEventClient medicalRecord,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthenticationService> logger)
    {
        _authUsers = authUsers;
        _refreshTokens = refreshTokens;
        _passwordResets = passwordResets;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _userServiceClient = userServiceClient;
        _esiaOAuthService = esiaOAuthService;
        _medicalRecord = medicalRecord;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    public async Task<TokenPairResponse> RegisterAsync(
        RegisterRequest request,
        string? ipAddress,
        string? deviceFingerprint,
        CancellationToken cancellationToken = default)
    {
        var phone = PhoneNormalizer.Normalize(request.PhoneNumber);
        if (await _authUsers.PhoneExistsAsync(phone, cancellationToken))
            throw new DuplicatePhoneException(request.PhoneNumber);

        var user = await _userServiceClient.RegisterUserAsync(request, cancellationToken);
        var (hash, salt) = _passwordHasher.HashPassword(request.Password);

        var authUser = new AuthUser
        {
            UserPublicId = user.PublicId,
            NormalizedPhone = phone,
            PasswordHash = hash,
            PasswordSalt = salt
        };

        await _authUsers.AddAsync(authUser, cancellationToken);
        await _authUsers.SaveChangesAsync(cancellationToken);

        return await IssueTokenPairAsync(authUser, ipAddress, deviceFingerprint, cancellationToken);
    }

    public async Task<TokenPairResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var authUser = await GetAuthUserByPhoneOrThrowAsync(request.PhoneNumber, cancellationToken);
        EnsureNotBlocked(authUser);

        if (!_passwordHasher.Verify(request.Password, authUser.PasswordHash, authUser.PasswordSalt))
            throw new InvalidCredentialsException();

        // Default login always activates Patient when present, so a dual-profile account
        // cannot silently enter as Doctor. Explicit preferredProfileType=Doctor is required.
        var preferred = string.IsNullOrWhiteSpace(request.PreferredProfileType)
            ? "Patient"
            : request.PreferredProfileType.Trim();

        try
        {
            await _userServiceClient.ActivateProfileByTypeAsync(
                authUser.UserPublicId,
                preferred,
                cancellationToken);
        }
        catch (AuthValidationException) when (preferred.Equals("Patient", StringComparison.OrdinalIgnoreCase))
        {
            // Account may be doctor-only — keep whatever profile is already active.
        }

        return await IssueTokenPairAsync(authUser, ipAddress, request.DeviceFingerprint, cancellationToken);
    }

    public async Task<TokenPairResponse> SwitchProfileAsync(
        Guid userPublicId,
        SwitchProfileRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();
        EnsureNotBlocked(authUser);

        await _userServiceClient.SwitchActiveProfileAsync(userPublicId, request.ProfileId, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
            if (stored is not null && !stored.IsRevoked)
            {
                stored.IsRevoked = true;
                await _refreshTokens.SaveChangesAsync(cancellationToken);
            }
        }

        return await IssueTokenPairAsync(authUser, ipAddress, request.DeviceFingerprint, cancellationToken);
    }

    public async Task<TokenPairResponse> RefreshAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (stored is null || stored.IsRevoked || stored.ExpiresAt <= DateTime.UtcNow)
            throw new InvalidRefreshTokenException();

        if (!string.IsNullOrEmpty(request.DeviceFingerprint) &&
            !string.IsNullOrEmpty(stored.DeviceFingerprint) &&
            stored.DeviceFingerprint != request.DeviceFingerprint)
            throw new InvalidRefreshTokenException();

        stored.IsRevoked = true;
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return await IssueTokenPairAsync(stored.AuthUser, ipAddress, request.DeviceFingerprint, cancellationToken);
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (stored is null)
            return;

        stored.IsRevoked = true;
        await _refreshTokens.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userPublicId,
        ChangePasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        EnsureNotBlocked(authUser);

        if (!_passwordHasher.Verify(request.CurrentPassword, authUser.PasswordHash, authUser.PasswordSalt))
            throw new InvalidCredentialsException();

        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        authUser.PasswordHash = hash;
        authUser.PasswordSalt = salt;
        authUser.UpdatedAt = DateTime.UtcNow;

        await _refreshTokens.RevokeAllForUserAsync(authUser.Id, cancellationToken);
        await _authUsers.SaveChangesAsync(cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var authUser = await GetAuthUserByPhoneOrThrowAsync(request.PhoneNumber, cancellationToken);
        var code = Random.Shared.Next(100000, 999999).ToString();

        await _passwordResets.InvalidateActiveForUserAsync(authUser.Id, cancellationToken);
        await _passwordResets.AddAsync(new PasswordResetToken
        {
            AuthUserId = authUser.Id,
            Code = code,
            Channel = request.Channel,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        }, cancellationToken);
        await _passwordResets.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Password reset code for {Phone} via {Channel}: {Code} (dev/log stub; integrate SMS/email provider)",
            authUser.NormalizedPhone,
            request.Channel,
            code);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var authUser = await GetAuthUserByPhoneOrThrowAsync(request.PhoneNumber, cancellationToken);
        var reset = await _passwordResets.GetActiveByCodeAsync(authUser.Id, request.Code, cancellationToken)
            ?? throw new AuthValidationException("Invalid or expired reset code.");

        var (hash, salt) = _passwordHasher.HashPassword(request.NewPassword);
        authUser.PasswordHash = hash;
        authUser.PasswordSalt = salt;
        authUser.UpdatedAt = DateTime.UtcNow;

        reset.IsUsed = true;
        await _refreshTokens.RevokeAllForUserAsync(authUser.Id, cancellationToken);
        await _authUsers.SaveChangesAsync(cancellationToken);
    }

    public async Task<TokenPairResponse> CompleteEsiaStubRegisterAsync(
        EsiaStubRegisterRequest request,
        string? ipAddress,
        string? deviceFingerprint,
        CancellationToken cancellationToken = default)
    {
        if (!_esiaOAuthService.UseStub)
            throw new EsiaNotConfiguredException();

        ValidateStubRegister(request);
        var phone = PhoneNormalizer.Normalize(request.PhoneNumber);
        var existed = await _authUsers.GetByPhoneAsync(phone, cancellationToken) is not null
                      || await _authUsers.GetByEsiaSubjectIdAsync(
                          EsiaStubProfileFactory.SubjectIdForPhone(phone), cancellationToken) is not null;

        var esiaUser = existed
            ? EsiaStubProfileFactory.ForExistingAccount(phone)
            : EsiaStubProfileFactory.ForRegister(
                request.LastName, request.FirstName, request.MiddleName, request.Email, request.PhoneNumber);

        var authUser = await FindOrCreateUserForEsiaAsync(
            esiaUser, cancellationToken, EsiaStubProfileFactory.DevPassword);
        EnsureNotBlocked(authUser);
        var tokens = await IssueTokenPairAsync(authUser, ipAddress, deviceFingerprint, cancellationToken);
        tokens.Esia = await SyncEsiaProfileAsync(authUser, esiaUser, cancellationToken);
        tokens.Esia.ExistingAccount = existed;
        tokens.Esia.DevPassword = existed ? null : EsiaStubProfileFactory.DevPassword;
        return tokens;
    }

    public async Task<TokenPairResponse> LinkEsiaStubAsync(
        Guid userPublicId,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (!_esiaOAuthService.UseStub)
            throw new EsiaNotConfiguredException();

        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();
        EnsureNotBlocked(authUser);

        var esiaUser = EsiaStubProfileFactory.ForLink(authUser.NormalizedPhone);
        await AttachEsiaLinkAsync(authUser, esiaUser, cancellationToken);
        var tokens = await IssueTokenPairAsync(authUser, ipAddress, null, cancellationToken);
        tokens.Esia = await SyncEsiaProfileAsync(authUser, esiaUser, cancellationToken);
        tokens.Esia.ExistingAccount = true;
        return tokens;
    }

    public async Task<TokenPairResponse> CompleteEsiaLoginAsync(
        string code,
        string? ipAddress,
        string? deviceFingerprint,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        var esiaUser = await _esiaOAuthService.ExchangeCodeAsync(code, state, cancellationToken);
        var authUser = await FindOrCreateUserForEsiaAsync(esiaUser, cancellationToken);
        EnsureNotBlocked(authUser);
        var tokens = await IssueTokenPairAsync(authUser, ipAddress, deviceFingerprint, cancellationToken);
        tokens.Esia = await SyncEsiaProfileAsync(authUser, esiaUser, cancellationToken);
        return tokens;
    }

    public async Task LinkEsiaAsync(
        Guid userPublicId,
        string code,
        string? currentPassword,
        string? state = null,
        CancellationToken cancellationToken = default)
    {
        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (!string.IsNullOrWhiteSpace(currentPassword) &&
            !_passwordHasher.Verify(currentPassword, authUser.PasswordHash, authUser.PasswordSalt))
            throw new InvalidCredentialsException();

        var esiaUser = await _esiaOAuthService.ExchangeCodeAsync(code, state, cancellationToken);
        await AttachEsiaLinkAsync(authUser, esiaUser, cancellationToken);
        await SyncEsiaProfileAsync(authUser, esiaUser, cancellationToken);
    }

    public async Task<TokenPairResponse> CompleteEsiaSessionAsync(
        EsiaAuthSession session,
        string code,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        if (session.Intent.Equals("link", StringComparison.OrdinalIgnoreCase))
        {
            if (session.UserPublicId is null || session.UserPublicId == Guid.Empty)
                throw new AuthValidationException("ESIA link requires an authenticated user.");

            var authUser = await _authUsers.GetByUserPublicIdAsync(session.UserPublicId.Value, cancellationToken)
                ?? throw new InvalidCredentialsException();
            EnsureNotBlocked(authUser);

            var esiaUser = await _esiaOAuthService.ExchangeCodeAsync(code, session.State, cancellationToken);
            await AttachEsiaLinkAsync(authUser, esiaUser, cancellationToken);
            var tokens = await IssueTokenPairAsync(authUser, ipAddress, session.DeviceFingerprint, cancellationToken);
            tokens.Esia = await SyncEsiaProfileAsync(authUser, esiaUser, cancellationToken);
            return tokens;
        }

        return await CompleteEsiaLoginAsync(code, ipAddress, session.DeviceFingerprint, session.State, cancellationToken);
    }

    public async Task<EsiaStatusResponse> GetEsiaStatusAsync(Guid userPublicId, CancellationToken cancellationToken = default)
    {
        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        var link = authUser.EsiaLink;
        return new EsiaStatusResponse
        {
            Linked = link is not null,
            LinkedAt = link?.LinkedAt,
            SnilsMasked = MaskSnils(link?.Snils)
        };
    }

    private async Task AttachEsiaLinkAsync(AuthUser authUser, EsiaUserInfo esiaUser, CancellationToken cancellationToken)
    {
        var existing = await _authUsers.GetByEsiaSubjectIdAsync(esiaUser.SubjectId, cancellationToken);
        if (existing is not null && existing.Id != authUser.Id)
            throw new AuthValidationException("Этот аккаунт Госуслуг уже привязан к другому пользователю.");

        if (authUser.EsiaLink is null)
        {
            authUser.EsiaLink = new EsiaLink
            {
                AuthUserId = authUser.Id,
                EsiaSubjectId = esiaUser.SubjectId,
                Snils = esiaUser.Snils,
                LinkedAt = DateTime.UtcNow
            };
        }
        else
        {
            authUser.EsiaLink.EsiaSubjectId = esiaUser.SubjectId;
            authUser.EsiaLink.Snils = esiaUser.Snils;
            authUser.EsiaLink.LinkedAt = DateTime.UtcNow;
        }

        authUser.UpdatedAt = DateTime.UtcNow;
        await _authUsers.SaveChangesAsync(cancellationToken);
    }

    private async Task<EsiaSyncResultDto> SyncEsiaProfileAsync(
        AuthUser authUser,
        EsiaUserInfo esiaUser,
        CancellationToken cancellationToken)
    {
        var summary = new EsiaSyncResultDto
        {
            Linked = true,
            FullName = string.Join(" ", new[] { esiaUser.LastName, esiaUser.FirstName, esiaUser.MiddleName }
                .Where(x => !string.IsNullOrWhiteSpace(x))),
            OmsImported = !string.IsNullOrWhiteSpace(esiaUser.OmsNumber),
            AddressImported = esiaUser.ResidenceAddress is not null || esiaUser.RegistrationAddress is not null
        };

        try
        {
            await _userServiceClient.ApplyEsiaProfileAsync(authUser.UserPublicId, esiaUser, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to apply ESIA profile to UserService for {UserId}", authUser.UserPublicId);
        }

        try
        {
            await _medicalRecord.ImportEsiaSnapshotAsync(authUser.UserPublicId, esiaUser, cancellationToken);
            summary.MedicalRecordSnapshotWritten = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to write ESIA snapshot to medical record for {UserId}", authUser.UserPublicId);
        }

        return summary;
    }

    private static string? MaskSnils(string? snils)
    {
        if (string.IsNullOrWhiteSpace(snils))
            return null;
        var digits = new string(snils.Where(char.IsDigit).ToArray());
        return digits.Length < 4 ? "***" : $"***{digits[^4..]}";
    }

    private async Task<AuthUser> FindOrCreateUserForEsiaAsync(
        EsiaUserInfo esiaUser,
        CancellationToken cancellationToken,
        string? initialPassword = null)
    {
        var byEsia = await _authUsers.GetByEsiaSubjectIdAsync(esiaUser.SubjectId, cancellationToken);
        if (byEsia is not null)
            return byEsia;

        if (!string.IsNullOrWhiteSpace(esiaUser.Phone))
        {
            var phone = PhoneNormalizer.Normalize(esiaUser.Phone);
            var byPhone = await _authUsers.GetByPhoneAsync(phone, cancellationToken);
            if (byPhone is not null)
            {
                await AttachEsiaLinkAsync(byPhone, esiaUser, cancellationToken);
                return byPhone;
            }
        }

        var password = string.IsNullOrWhiteSpace(initialPassword)
            ? Guid.NewGuid().ToString("N") + "Aa1!"
            : initialPassword;

        var registerRequest = new RegisterRequest
        {
            PhoneNumber = esiaUser.Phone ?? throw new AuthValidationException("В профиле Госуслуг нет телефона — добавьте мобильный в ЕСИА и повторите."),
            Password = password,
            Email = esiaUser.Email,
            FirstName = esiaUser.FirstName ?? "Unknown",
            SecondName = esiaUser.MiddleName,
            Surename = esiaUser.LastName ?? "Unknown",
            BirthDate = DateTime.SpecifyKind((esiaUser.BirthDate ?? DateTime.UtcNow.Date.AddYears(-30)).Date, DateTimeKind.Utc),
            Sex = MapRegisterSex(esiaUser.Gender),
            PatientProfile = new
            {
                snils = NormalizeSnils(esiaUser.Snils),
                insuranceNumber = esiaUser.OmsNumber,
                residenceAddress = esiaUser.ResidenceAddress is null ? null : new
                {
                    postCode = esiaUser.ResidenceAddress.PostCode,
                    country = esiaUser.ResidenceAddress.Country,
                    region = esiaUser.ResidenceAddress.Region,
                    city = esiaUser.ResidenceAddress.City,
                    area = esiaUser.ResidenceAddress.Area,
                    street = esiaUser.ResidenceAddress.Street ?? esiaUser.ResidenceAddress.AddressStr,
                    house = esiaUser.ResidenceAddress.House,
                    flat = esiaUser.ResidenceAddress.Flat
                },
                registrationAddress = esiaUser.RegistrationAddress is null ? null : new
                {
                    postCode = esiaUser.RegistrationAddress.PostCode,
                    country = esiaUser.RegistrationAddress.Country,
                    region = esiaUser.RegistrationAddress.Region,
                    city = esiaUser.RegistrationAddress.City,
                    area = esiaUser.RegistrationAddress.Area,
                    street = esiaUser.RegistrationAddress.Street ?? esiaUser.RegistrationAddress.AddressStr,
                    house = esiaUser.RegistrationAddress.House,
                    flat = esiaUser.RegistrationAddress.Flat
                }
            }
        };

        var user = await _userServiceClient.RegisterUserAsync(registerRequest, cancellationToken);
        var (hash, salt) = _passwordHasher.HashPassword(registerRequest.Password);
        var authUser = new AuthUser
        {
            UserPublicId = user.PublicId,
            NormalizedPhone = PhoneNormalizer.Normalize(registerRequest.PhoneNumber),
            PasswordHash = hash,
            PasswordSalt = salt,
            EsiaLink = new EsiaLink
            {
                EsiaSubjectId = esiaUser.SubjectId,
                Snils = esiaUser.Snils
            }
        };

        await _authUsers.AddAsync(authUser, cancellationToken);
        await _authUsers.SaveChangesAsync(cancellationToken);
        return authUser;
    }

    private static void ValidateStubRegister(EsiaStubRegisterRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.LastName) ||
            string.IsNullOrWhiteSpace(request.FirstName))
            throw new AuthValidationException("Укажите фамилию и имя.");
        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            throw new AuthValidationException("Укажите корректную почту.");
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            throw new AuthValidationException("Укажите телефон.");
        var phone = PhoneNormalizer.Normalize(request.PhoneNumber);
        if (phone.Length < 11)
            throw new AuthValidationException("Укажите телефон в формате 7XXXXXXXXXX.");
    }

    private async Task<TokenPairResponse> IssueTokenPairAsync(
        AuthUser authUser,
        string? ipAddress,
        string? deviceFingerprint,
        CancellationToken cancellationToken)
    {
        var rolesData = await _userServiceClient.GetRolesAndPermissionsAsync(authUser.UserPublicId, cancellationToken);
        var accessToken = _jwtTokenService.CreateAccessToken(
            authUser.UserPublicId,
            rolesData.Roles,
            rolesData.Permissions,
            rolesData.ProfileIds);

        var refreshTokenValue = Guid.NewGuid().ToString("N");
        var refresh = new RefreshToken
        {
            AuthUserId = authUser.Id,
            Token = refreshTokenValue,
            DeviceFingerprint = deviceFingerprint,
            IpAddress = ipAddress,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays)
        };

        await _refreshTokens.AddAsync(refresh, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return new TokenPairResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshTokenValue,
            ExpiresIn = _jwtOptions.AccessTokenMinutes * 60,
            UserPublicId = authUser.UserPublicId
        };
    }

    private async Task<AuthUser> GetAuthUserByPhoneOrThrowAsync(string phone, CancellationToken cancellationToken)
    {
        var normalized = PhoneNormalizer.Normalize(phone);
        return await _authUsers.GetByPhoneAsync(normalized, cancellationToken)
            ?? throw new InvalidCredentialsException();
    }

    private static void EnsureNotBlocked(AuthUser authUser)
    {
        if (authUser.IsBlocked)
            throw new AccountBlockedException();
    }

    private static string MapRegisterSex(string? gender) => gender?.Trim().ToUpperInvariant() switch
    {
        "F" or "FEMALE" or "ЖЕН" or "ЖЕНСКИЙ" => "Female",
        _ => "Male"
    };

    private static string? NormalizeSnils(string? snils)
    {
        if (string.IsNullOrWhiteSpace(snils))
            return null;
        var digits = new string(snils.Where(char.IsDigit).ToArray());
        return digits.Length == 11 ? digits : null;
    }
}
