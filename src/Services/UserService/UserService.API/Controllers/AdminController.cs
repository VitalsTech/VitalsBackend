using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Policy = "InternalService")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminUserService _adminUserService;
        private readonly IPermissionService _permissionService;

        public AdminController(IAdminUserService adminUserService, IPermissionService permissionService)
        {
            _adminUserService = adminUserService;
            _permissionService = permissionService;
        }

        [HttpPost("users/search")]
        public async Task<ActionResult<SearchResult<UserDto>>> SearchUsers(UserSearchRequest request)
        {
            var result = await _adminUserService.SearchUsersAsync(request);
            return Ok(result);
        }

        [HttpPut("users/{publicId:guid}/status")]
        public async Task<IActionResult> SetUserStatus(Guid publicId, UserStatusRequest request)
        {
            var result = await _adminUserService.SetUserStatusAsync(publicId, request);
            if (!result)
                return NotFound($"User with ID {publicId} not found");
            return Ok(new { message = "User status updated successfully" });
        }

        [HttpPost("users/{publicId:guid}/block")]
        public async Task<IActionResult> BlockUser(Guid publicId, UserBlockRequest request)
        {
            var result = await _adminUserService.BlockUserAsync(publicId, request);
            if (!result)
                return NotFound($"User with ID {publicId} not found");
            return Ok(new { message = "User blocked successfully" });
        }

        [HttpPost("users/{publicId:guid}/unblock")]
        public async Task<IActionResult> UnblockUser(Guid publicId)
        {
            var result = await _adminUserService.UnblockUserAsync(publicId);
            if (!result)
                return NotFound($"User with ID {publicId} not found");
            return Ok(new { message = "User unblocked successfully" });
        }

        [HttpDelete("users/{publicId:guid}/soft")]
        public async Task<IActionResult> SoftDeleteUser(Guid publicId)
        {
            var result = await _adminUserService.SoftDeleteUserAsync(publicId);
            if (!result)
                return NotFound($"User with ID {publicId} not found");
            return Ok(new { message = "User soft deleted successfully" });
        }

        [HttpPost("users/{publicId:guid}/restore")]
        public async Task<IActionResult> RestoreUser(Guid publicId)
        {
            var result = await _adminUserService.RestoreUserAsync(publicId);
            if (!result)
                return NotFound($"User with ID {publicId} not found");
            return Ok(new { message = "User restored successfully" });
        }

        [HttpPost("permissions/check")]
        public async Task<ActionResult<PermissionCheckResponse>> CheckPermission(PermissionCheckRequest request)
        {
            var result = await _permissionService.CheckPermissionAsync(request);
            return Ok(result);
        }

        [HttpGet("permissions/user/{publicId:guid}")]
        public async Task<ActionResult<UserRoleResponse>> GetUserPermissions(Guid publicId)
        {
            var result = await _permissionService.GetUserRolesAndPermissionsAsync(publicId);
            return Ok(result);
        }
    }

    public class SearchResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }
}