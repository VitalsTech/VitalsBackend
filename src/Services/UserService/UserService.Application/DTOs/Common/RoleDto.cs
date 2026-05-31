namespace UserService.Application.DTOs.Common
{
    public class RoleDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> Permissions { get; set; } = new();
    }

    public class PermissionCheckRequest
    {
        public Guid UserPublicId { get; set; }
        public string Permission { get; set; } = string.Empty;
    }

    public class PermissionCheckResponse
    {
        public bool HasPermission { get; set; }
        public string? Reason { get; set; }
    }

    public class UserRoleResponse
    {
        public Guid UserPublicId { get; set; }
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }

    public class UserSearchRequest
    {
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? FirstName { get; set; }
        public string? Surename { get; set; }
        public string? UserType { get; set; }
        public string? Specialization { get; set; }
        public string? OrganizationRole { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 20;
    }

    public class UserStatusRequest
    {
        public bool IsActive { get; set; }
        public string? Reason { get; set; }
    }

    public class UserBlockRequest
    {
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
    }
}
