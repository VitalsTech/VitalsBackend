using UserService.Application.DTOs.Common;

namespace UserService.Application.Interfaces
{
    public interface IPermissionService
    {
        Task<UserRoleResponse> GetUserRolesAndPermissionsAsync(Guid publicId);
        Task<bool> HasRoleAsync(Guid publicId, string role);
        Task<bool> HasPermissionAsync(Guid publicId, string permission);
        Task<PermissionCheckResponse> CheckPermissionAsync(PermissionCheckRequest request);
    }
    public interface IAdminUserService
    {
        Task<SearchResult<UserDto>> SearchUsersAsync(UserSearchRequest request);
        Task<bool> SetUserStatusAsync(Guid publicId, UserStatusRequest request);
        Task<bool> BlockUserAsync(Guid publicId, UserBlockRequest request);
        Task<bool> UnblockUserAsync(Guid publicId);
        Task<bool> SoftDeleteUserAsync(Guid publicId);
        Task<bool> RestoreUserAsync(Guid publicId);
    }

    public class SearchResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}