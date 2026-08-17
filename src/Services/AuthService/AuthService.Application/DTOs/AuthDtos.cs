namespace AuthService.Application.DTOs;

public sealed class TokenPairResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public Guid UserPublicId { get; set; }
    public EsiaSyncResultDto? Esia { get; set; }
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
    /// <summary>Optional: Patient | Doctor | Organization. Activates matching profile before issuing JWT.</summary>
    public string? PreferredProfileType { get; set; }
}

public sealed class SwitchProfileRequest
{
    public Guid ProfileId { get; set; }
    public string? DeviceFingerprint { get; set; }
    public string? RefreshToken { get; set; }
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

public sealed class EsiaStartRequest
{
    /// <summary>register — новый аккаунт; link — привязка к текущему JWT.</summary>
    public string Intent { get; set; } = "register";
    public string? ReturnUrl { get; set; }
    /// <summary>fragment (default) | query — куда положить токены при редиректе на returnUrl.</summary>
    public string? ResponseMode { get; set; }
}

public sealed class EsiaStartResponse
{
    public string AuthorizationUrl { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Intent { get; set; } = "register";
    public bool Enabled { get; set; }
    public string Portal { get; set; } = string.Empty;
}

public sealed class EsiaConfigResponse
{
    public bool Enabled { get; set; }
    /// <summary>stub — заглушка DEV; oauth — живой портал (сейчас не используется на DEV).</summary>
    public string Mode { get; set; } = "stub";
    public bool Configured { get; set; }
    public string Portal { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public IReadOnlyList<string> Intents { get; set; } = ["register", "link"];
    public IReadOnlyList<string> Collects { get; set; } = ["fullName", "email", "phone"];
    public IReadOnlyList<string> Generates { get; set; } =
        ["oms", "snils", "birthDate", "gender", "residenceAddress", "registrationAddress", "medicalRecordSnapshot"];
    public IReadOnlyList<string> Skipped { get; set; } = ["clinics", "doctors"];
}

public sealed class EsiaStatusResponse
{
    public bool Linked { get; set; }
    public DateTime? LinkedAt { get; set; }
    public string? SnilsMasked { get; set; }
}

public sealed class EsiaStubRegisterRequest
{
    public string LastName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
}

public sealed class EsiaCompleteRequest
{
    public string Code { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
}

public sealed class EsiaSyncResultDto
{
    public bool Linked { get; set; }
    public string? FullName { get; set; }
    public bool OmsImported { get; set; }
    public bool AddressImported { get; set; }
    public bool MedicalRecordSnapshotWritten { get; set; }
    /// <summary>true — вошли в уже существующий аккаунт с этим телефоном (не создали второй).</summary>
    public bool ExistingAccount { get; set; }
    /// <summary>Только если аккаунт создан через заглушку Госуслуг. Пароль для последующего входа по телефону.</summary>
    public string? DevPassword { get; set; }
    public IReadOnlyList<string> Skipped { get; set; } = ["clinics", "doctors"];
}

public sealed class EsiaAddressInfo
{
    public string? Type { get; set; }
    public string? PostCode { get; set; }
    public string? Country { get; set; }
    public string? Region { get; set; }
    public string? City { get; set; }
    public string? Area { get; set; }
    public string? Street { get; set; }
    public string? House { get; set; }
    public string? Flat { get; set; }
    public string? AddressStr { get; set; }
}

public sealed class EsiaDocumentInfo
{
    public string? Type { get; set; }
    public string? Series { get; set; }
    public string? Number { get; set; }
    public string? IssueDate { get; set; }
    public string? IssuedBy { get; set; }
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
    public string? OmsNumber { get; set; }
    public string? OmsSeries { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? Gender { get; set; }
    public EsiaAddressInfo? ResidenceAddress { get; set; }
    public EsiaAddressInfo? RegistrationAddress { get; set; }
    public IReadOnlyList<EsiaDocumentInfo> MedicalDocuments { get; set; } = Array.Empty<EsiaDocumentInfo>();
}
