using Microsoft.AspNetCore.Mvc;
using UserService.Application.DTOs.Common;
using UserService.Application.Interfaces;
using UserService.Application.Services;
using UserService.Domain.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("internal")]
    internal class InternalController : ControllerBase
    {
        private readonly IMultiProfileUserService _multiProfileUserService;
        private readonly IPermissionService _permissionService;

        public InternalController(
            IMultiProfileUserService multiProfileUserService,
            IPermissionService permissionService)
        {
            _multiProfileUserService = multiProfileUserService;
            _permissionService = permissionService;
        }

        // Получение пользователя по телефону (возвращает все профили)
        [HttpGet("users/by-phone/{phone}")]
        public async Task<ActionResult<UserWithProfilesDto>> GetUserByPhone(string phone)
        {
            // Нужно добавить метод GetByPhoneAsync в IMultiProfileUserService
            // Пока используем прямой поиск через репозиторий
            // Для простоты можно временно оставить только проверку существования
            return Ok(new { phone, message = "Method to be implemented" });
        }

        // Получение пользователя по email
        [HttpGet("users/by-email/{email}")]
        public async Task<ActionResult<UserWithProfilesDto>> GetUserByEmail(string email)
        {
            return Ok(new { email, message = "Method to be implemented" });
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
    }
}