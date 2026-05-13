using AutoMapper;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;

namespace UserService.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMultiProfileUserService _multiProfileUserService;

        public PermissionService(
            IUserRepository userRepository,
            IMultiProfileUserService multiProfileUserService)
        {
            _userRepository = userRepository;
            _multiProfileUserService = multiProfileUserService;
        }

        public async Task<UserRoleResponse> GetUserRolesAndPermissionsAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return new UserRoleResponse { UserPublicId = publicId };

            var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
            var activeProfile = profiles.FirstOrDefault(p => p.IsActive);

            if (activeProfile == null)
            {
                return new UserRoleResponse
                {
                    UserPublicId = publicId,
                    Roles = new List<string>(),
                    Permissions = new List<string>()
                };
            }

            var roles = new List<string> { activeProfile.ProfileType };
            var permissions = GetPermissionsForProfileType(activeProfile.ProfileType);

            if (user.Email == "admin@vitals.com")
            {
                roles.Add("Admin");
                permissions.AddRange(GetAdminPermissions());
            }

            return new UserRoleResponse
            {
                UserPublicId = publicId,
                Roles = roles,
                Permissions = permissions.Distinct().ToList()
            };
        }

        public async Task<bool> HasRoleAsync(Guid publicId, string role)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
            var activeProfile = profiles.FirstOrDefault(p => p.IsActive);

            if (activeProfile == null)
                return false;

            if (activeProfile.ProfileType.Equals(role, StringComparison.OrdinalIgnoreCase))
                return true;

            if (role == "Admin" && user.Email == "admin@vitals.com")
                return true;

            return false;
        }

        public async Task<bool> HasPermissionAsync(Guid publicId, string permission)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                return false;

            if (!user.IsActive || (user.BlockedUntil.HasValue && user.BlockedUntil.Value > DateTime.UtcNow))
                return false;

            var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
            var activeProfile = profiles.FirstOrDefault(p => p.IsActive);

            if (activeProfile == null)
                return false;

            var permissions = GetPermissionsForProfileType(activeProfile.ProfileType);

            if (permissions.Contains(permission))
                return true;

            if (user.Email == "admin@vitals.com")
            {
                var adminPermissions = GetAdminPermissions();
                if (adminPermissions.Contains(permission))
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

            if (user != null && (!user.IsActive || (user.BlockedUntil.HasValue && user.BlockedUntil.Value > DateTime.UtcNow)))
                reason = "User is blocked or inactive";

            return new PermissionCheckResponse
            {
                HasPermission = false,
                Reason = reason
            };
        }

        private List<string> GetPermissionsForProfileType(string profileType)
        {
            return profileType switch
            {
                "Patient" => new List<string>
                {
                    "profile.view.self",
                    "profile.edit.self",
                    "medical.history.view",
                    "prescription.view",
                    "prescription.fill",
                    "consultation.start",
                    "consultation.join",
                    "lab.order.view",
                    "lab.result.view"
                },
                "Doctor" => new List<string>
                {
                    "profile.view.self",
                    "profile.edit.self",
                    "profile.view.patient",
                    "medical.history.view.all",
                    "prescription.create",
                    "prescription.view.all",
                    "prescription.cancel",
                    "consultation.start",
                    "consultation.join",
                    "consultation.end",
                    "lab.order.create",
                    "lab.result.view",
                    "certificate.view",
                    "certificate.update"
                },
                "Organization" => new List<string>
                {
                    "profile.view.self",
                    "profile.edit.self",
                    "org.doctors.manage",
                    "org.schedule.manage",
                    "org.patients.view",
                    "org.finance.view",
                    "org.reports.generate",
                    "integration.settings.manage"
                },
                _ => new List<string>()
            };
        }

        private List<string> GetAdminPermissions()
        {
            return new List<string>
            {
                "users.view.all",
                "users.block",
                "users.delete",
                "audit.log.view",
                "system.settings.manage"
            };
        }
    }
}