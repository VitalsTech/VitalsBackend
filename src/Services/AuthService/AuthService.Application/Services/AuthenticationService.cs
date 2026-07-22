using AuthService.Application.DTOs;
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

    public async Task<TokenPairResponse> CompleteEsiaLoginAsync(
        string code,
        string? ipAddress,
        string? deviceFingerprint,
        CancellationToken cancellationToken = default)
    {
        var esiaUser = await _esiaOAuthService.ExchangeCodeAsync(code, cancellationToken);
        var authUser = await FindOrCreateUserForEsiaAsync(esiaUser, cancellationToken);
        EnsureNotBlocked(authUser);
        return await IssueTokenPairAsync(authUser, ipAddress, deviceFingerprint, cancellationToken);
    }

    public async Task LinkEsiaAsync(
        Guid userPublicId,
        string code,
        string currentPassword,
        CancellationToken cancellationToken = default)
    {
        var authUser = await _authUsers.GetByUserPublicIdAsync(userPublicId, cancellationToken)
            ?? throw new InvalidCredentialsException();

        if (!_passwordHasher.Verify(currentPassword, authUser.PasswordHash, authUser.PasswordSalt))
            throw new InvalidCredentialsException();

        var esiaUser = await _esiaOAuthService.ExchangeCodeAsync(code, cancellationToken);
        var existing = await _authUsers.GetByEsiaSubjectIdAsync(esiaUser.SubjectId, cancellationToken);
        if (existing is not null && existing.Id != authUser.Id)
            throw new AuthValidationException("This ESIA account is already linked to another user.");

        authUser.EsiaLink = new EsiaLink
        {
            AuthUserId = authUser.Id,
            EsiaSubjectId = esiaUser.SubjectId,
            Snils = esiaUser.Snils,
            LinkedAt = DateTime.UtcNow
        };

        await _authUsers.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthUser> FindOrCreateUserForEsiaAsync(EsiaUserInfo esiaUser, CancellationToken cancellationToken)
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
                byPhone.EsiaLink = new EsiaLink
                {
                    AuthUserId = byPhone.Id,
                    EsiaSubjectId = esiaUser.SubjectId,
                    Snils = esiaUser.Snils
                };
                await _authUsers.SaveChangesAsync(cancellationToken);
                return byPhone;
            }
        }

        var registerRequest = new RegisterRequest
        {
            PhoneNumber = esiaUser.Phone ?? throw new AuthValidationException("ESIA profile does not contain a phone number."),
            Password = Guid.NewGuid().ToString("N") + "Aa1!",
            Email = esiaUser.Email,
            FirstName = esiaUser.FirstName ?? "Unknown",
            SecondName = esiaUser.MiddleName,
            Surename = esiaUser.LastName ?? "Unknown",
            BirthDate = DateTime.UtcNow.Date.AddYears(-30),
            Sex = "Male",
            PatientProfile = new { }
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
}
