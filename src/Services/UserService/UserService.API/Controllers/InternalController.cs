using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Application.Exceptions;
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

        [HttpPost("users/{publicId:guid}/switch-profile")]
        public async Task<ActionResult<ActiveProfileResponse>> SwitchProfileInternal(
            Guid publicId,
            [FromBody] SwitchActiveProfileRequest request)
        {
            request.UserPublicId = publicId;
            try
            {
                if (request.ProfileId == Guid.Empty && !string.IsNullOrWhiteSpace(request.ProfileType))
                {
                    var profiles = await _multiProfileUserService.GetUserProfilesAsync(publicId);
                    var match = profiles.FirstOrDefault(p =>
                        p.ProfileType.Equals(request.ProfileType, StringComparison.OrdinalIgnoreCase));
                    if (match is null)
                        return NotFound(new { error = $"Profile type {request.ProfileType} not found for user." });
                    request.ProfileId = match.ProfileId;
                }

                return Ok(await _multiProfileUserService.SwitchActiveProfileAsync(request));
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
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