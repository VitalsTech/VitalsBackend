using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/admin/roles")]
    [Authorize(Policy = "InternalService")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPermissionRepository _permissionRepository;

        public RolesController(
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository,
            IUserRepository userRepository,
            IPermissionRepository permissionRepository)
        {
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
            _userRepository = userRepository;
            _permissionRepository = permissionRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Role>>> GetAllRoles()
        {
            var roles = await _roleRepository.GetAllAsync();
            return Ok(roles);
        }

        [HttpGet("permissions")]
        public async Task<ActionResult<IEnumerable<Permission>>> GetAllPermissions()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return Ok(permissions);
        }

        [HttpPost("users/{publicId}/assign")]
        public async Task<IActionResult> AssignRole(Guid publicId, [FromBody] AssignRoleRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return NotFound($"User with ID {publicId} not found");

            var role = await _roleRepository.GetByNameAsync(request.RoleName);
            if (role == null)
                return NotFound($"Role '{request.RoleName}' not found");

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            var profile = profiles.FirstOrDefault(p => p.Id == request.ProfileId);
            if (profile == null)
                return NotFound($"Profile with ID {request.ProfileId} not found");

            var existing = await _userRoleRepository.GetAsync(user.Id, role.Id, profile.Id);
            if (existing != null)
                return Conflict("User already has this role");

            var userRole = new UserRole
            {
                UserId = user.Id,
                RoleId = role.Id,
                ProfileId = profile.Id,
                AssignedBy = null // TODO: получить ID текущего пользователя
            };

            await _userRoleRepository.AddAsync(userRole);
            await _userRepository.SaveChangesAsync();

            return Ok(new { message = $"Role '{request.RoleName}' assigned successfully" });
        }

        [HttpDelete("users/{publicId}/unassign")]
        public async Task<IActionResult> UnassignRole(Guid publicId, [FromBody] UnassignRoleRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return NotFound($"User with ID {publicId} not found");

            var role = await _roleRepository.GetByNameAsync(request.RoleName);
            if (role == null)
                return NotFound($"Role '{request.RoleName}' not found");

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            var profile = profiles.FirstOrDefault(p => p.Id == request.ProfileId);
            if (profile == null)
                return NotFound($"Profile with ID {request.ProfileId} not found");

            var userRole = await _userRoleRepository.GetAsync(user.Id, role.Id, profile.Id);
            if (userRole == null)
                return NotFound("User does not have this role");

            _userRoleRepository.Delete(userRole);
            await _userRepository.SaveChangesAsync();

            return Ok(new { message = $"Role '{request.RoleName}' unassigned successfully" });
        }

        [HttpGet("users/{publicId}")]
        public async Task<ActionResult<UserRolesResponse>> GetUserRoles(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return NotFound($"User with ID {publicId} not found");

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            var result = new UserRolesResponse { UserPublicId = publicId, Profiles = new List<ProfileRolesResponse>() };

            foreach (var profile in profiles)
            {
                var roles = await _userRoleRepository.GetRolesByProfileAsync(profile.Id);
                var permissions = await _permissionRepository.GetPermissionsByProfileAsync(profile.Id);
                
                result.Profiles.Add(new ProfileRolesResponse
                {
                    ProfileId = profile.Id,
                    ProfileType = profile.ProfileType.ToString(),
                    Roles = roles.Select(r => r.Name).ToList(),
                    Permissions = permissions.Select(p => p.Name).ToList()
                });
            }

            return Ok(result);
        }
    }

    public class AssignRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
    }

    public class UnassignRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
        public Guid ProfileId { get; set; }
    }

    public class UserRolesResponse
    {
        public Guid UserPublicId { get; set; }
        public List<ProfileRolesResponse> Profiles { get; set; } = new();
    }

    public class ProfileRolesResponse
    {
        public Guid ProfileId { get; set; }
        public string ProfileType { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();
    }
}