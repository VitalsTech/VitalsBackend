namespace ApiGateway.Application.DTOs.Admin;

public sealed class UserSearchRequestDto
{
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? FirstName { get; set; }
    public string? Surename { get; set; }
    public string? UserType { get; set; }
    public string? Specialization { get; set; }
    public string? OrganizationRole { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; } = 20;
}

public sealed class UserStatusRequestDto
{
    public bool IsActive { get; set; }
    public string? Reason { get; set; }
}

public sealed class UserBlockRequestDto
{
    public DateTime? BlockedUntil { get; set; }
    public string? BlockReason { get; set; }
}

public sealed class PermissionCheckRequestDto
{
    public Guid UserPublicId { get; set; }
    public string Permission { get; set; } = string.Empty;
}

public sealed class AssignRoleRequestDto
{
    public string RoleName { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
}

public sealed class UnassignRoleRequestDto
{
    public string RoleName { get; set; } = string.Empty;
    public Guid ProfileId { get; set; }
}
