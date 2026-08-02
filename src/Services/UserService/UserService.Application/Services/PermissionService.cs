using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IUserRoleRepository _userRoleRepository;
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepository,
            IPermissionRepository permissionRepository)
        {
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _permissionRepository = permissionRepository;
        }

        public async Task<UserRoleResponse> GetUserRolesAndPermissionsAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null || IsBlockedOrInactive(user))
                return new UserRoleResponse { UserPublicId = publicId };

            var profiles = (await _userRepository.GetProfilesByUserAsync(user.Id)).ToList();
            if (!profiles.Any())
                return new UserRoleResponse { UserPublicId = publicId };

            // Token roles must reflect the active profile only — otherwise a Patient can appear as Doctor.
            var activeProfiles = profiles.Where(p => p.IsActive).ToList();
            if (activeProfiles.Count == 0)
                activeProfiles = profiles.Take(1).ToList();

            var rolesList = new List<string>();
            var permissionsList = new List<string>();

            foreach (var profile in activeProfiles)
            {
                var roles = await _userRoleRepository.GetRolesByProfileAsync(profile.Id);
                rolesList.AddRange(roles.Select(r => r.Name));

                var permissions = await _permissionRepository.GetPermissionsByProfileAsync(profile.Id);
                permissionsList.AddRange(permissions.Select(p => p.Name));
            }

            return new UserRoleResponse
            {
                UserPublicId = publicId,
                Roles = rolesList.Distinct().ToList(),
                Permissions = permissionsList.Distinct().ToList(),
                ProfileIds = activeProfiles.Select(p => p.Id).ToList()
            };
        }

        public async Task<bool> HasRoleAsync(Guid publicId, string role)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null || IsBlockedOrInactive(user))
                return false;

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            
            foreach (var profile in profiles)
            {
                var roles = await _userRoleRepository.GetRolesByProfileAsync(profile.Id);
                if (roles.Any(r => r.Name == role))
                    return true;
            }

            return false;
        }

        public async Task<bool> HasPermissionAsync(Guid publicId, string permission)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            // Проверка блокировки
            if (IsBlockedOrInactive(user))
                return false;

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            
            foreach (var profile in profiles)
            {
                var permissions = await _permissionRepository.GetPermissionsByProfileAsync(profile.Id);
                if (permissions.Any(p => p.Name == permission))
                    return true;
            }

            return false;
        }

        public async Task<PermissionCheckResponse> CheckPermissionAsync(PermissionCheckRequest request)
        {
            var hasPermission = await HasPermissionAsync(request.UserPublicId, request.Permission);

            if (hasPermission)
                return new PermissionCheckResponse { HasPermission = true };

            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId);
            string reason = "User does not have required permission";

            if (user != null && IsBlockedOrInactive(user))
                reason = "User is blocked or inactive";

            return new PermissionCheckResponse
            {
                HasPermission = false,
                Reason = reason
            };
        }

        private static bool IsBlockedOrInactive(UserService.Domain.Entities.User user)
            => !user.IsActive || (user.BlockedUntil.HasValue && user.BlockedUntil.Value > DateTime.UtcNow);
    }
}
