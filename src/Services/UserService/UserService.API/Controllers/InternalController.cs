using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("internal")]
    [Authorize(Policy = "InternalService")]
    public class InternalController : ControllerBase
    {
        private readonly IMultiProfileUserService _multiProfileUserService;
        private readonly IPermissionService _permissionService;
        private readonly IDoctorScheduleService _doctorSchedule;

        public InternalController(
            IMultiProfileUserService multiProfileUserService,
            IPermissionService permissionService,
            IDoctorScheduleService doctorSchedule)
        {
            _multiProfileUserService = multiProfileUserService;
            _permissionService = permissionService;
            _doctorSchedule = doctorSchedule;
        }

        // Получение пользователя по телефону (возвращает все профили)
        [HttpGet("users/by-phone/{phone}")]
        public async Task<ActionResult<UserWithProfilesDto>> GetUserByPhone(string phone)
        {
            var user = await _multiProfileUserService.GetUserByPhoneAsync(phone);
            if (user is null)
                return NotFound();
            return Ok(user);
        }

        [HttpGet("users/by-email/{email}")]
        public async Task<ActionResult<UserWithProfilesDto>> GetUserByEmail(string email)
        {
            var user = await _multiProfileUserService.GetUserByEmailAsync(email);
            if (user is null)
                return NotFound();
            return Ok(user);
        }

        [HttpGet("users/{publicId:guid}/roles-permissions")]
        public async Task<ActionResult<UserRoleResponse>> GetRolesAndPermissions(Guid publicId)
        {
            var result = await _permissionService.GetUserRolesAndPermissionsAsync(publicId);
            return Ok(result);
        }

        // Проверка наличия права у пользователя
        [HttpGet("users/{publicId:guid}/has-permission/{permission}")]
        public async Task<ActionResult<bool>> HasPermission(Guid publicId, string permission)
        {
            var result = await _permissionService.HasPermissionAsync(publicId, permission);
            return Ok(result);
        }

        // Проверка существования пользователя
        [HttpGet("users/{publicId:guid}/exists")]
        public async Task<ActionResult<bool>> UserExists(Guid publicId)
        {
            try
            {
                var user = await _multiProfileUserService.GetUserWithProfilesAsync(publicId);
                return Ok(true);
            }
            catch
            {
                return Ok(false);
            }
        }

        // Получение всех профилей пользователя
        [HttpGet("users/{publicId:guid}/profiles")]
        public async Task<ActionResult<IEnumerable<ProfileInfoDto>>> GetUserProfiles(Guid publicId)
        {
            var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
            return Ok(profiles);
        }

        // Получение активного профиля пользователя
        [HttpGet("users/{publicId:guid}/active-profile")]
        public async Task<ActionResult<ProfileInfoDto>> GetActiveProfile(Guid publicId)
        {
            var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
            var activeProfile = profiles.FirstOrDefault(p => p.IsActive);

            if (activeProfile == null)
                return NotFound("No active profile found");

            return Ok(activeProfile);
        }

        [HttpGet("doctors/find-available")]
        public async Task<IActionResult> FindAvailableDoctor(
            [FromQuery] string specialty,
            [FromQuery] int urgencyLevel = 3,
            CancellationToken cancellationToken = default)
        {
            var doctor = await _doctorSchedule.FindAvailableDoctorAsync(specialty, urgencyLevel, cancellationToken);
            if (doctor is null)
                return NotFound();
            return Ok(doctor);
        }
    }
}