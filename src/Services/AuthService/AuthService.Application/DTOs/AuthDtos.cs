namespace AuthService.Application.DTOs;

public sealed class TokenPairResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserPublicId { get; set; }
}

public sealed class RegisterRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? SecondName { get; set; }
    public string Surename { get; set; } = string.Empty;
    public DateTime BirthDate { get; set; }
    public string Sex { get; set; } = string.Empty;
    public object? PatientProfile { get; set; }
    public object? DoctorProfile { get; set; }
    public object? OrganizationProfile { get; set; }
}

public sealed class LoginRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
}

public sealed class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
    public string? DeviceFingerprint { get; set; }
}

public sealed class LogoutRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public sealed class ChangePasswordRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ForgotPasswordRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Channel { get; set; } = "sms";
}

public sealed class ResetPasswordRequest
{
    public string PhoneNumber { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public sealed class ValidateTokenRequest
{
    public string Token { get; set; } = string.Empty;
}

public sealed class ValidateTokenResponse
{
    public bool IsValid { get; set; }
    public Guid? UserPublicId { get; set; }
    public IReadOnlyList<string> Roles { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> Scopes { get; set; } = Array.Empty<string>();
    public DateTime? ExpiresAt { get; set; }
    public string? Reason { get; set; }
}

public sealed class LinkEsiaRequest
{
    public string CurrentPassword { get; set; } = string.Empty;
}

public sealed class UserServiceUserDto
{
    public Guid PublicId { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public sealed class UserServiceRoleResponse
{
    public Guid UserPublicId { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
    public List<Guid> ProfileIds { get; set; } = new();
}

public sealed class EsiaUserInfo
{
    public string SubjectId { get; set; } = string.Empty;
    public string? Snils { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
}
