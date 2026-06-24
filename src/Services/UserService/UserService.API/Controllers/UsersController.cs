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
    [Route("api/users")]
    [Authorize(Policy = "InternalService")]
    public class UsersController : ControllerBase
    {
        private readonly IMultiProfileUserService _multiProfileUserService;

        public UsersController(IMultiProfileUserService multiProfileUserService)
        {
            _multiProfileUserService = multiProfileUserService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserWithProfilesDto>> Register(CreateUserWithProfileRequest request)
        {
            var result = await _multiProfileUserService.CreateUserWithProfileAsync(request);
            return Ok(result);
        }

        [HttpGet("{publicId:guid}")]
        public async Task<ActionResult<UserWithProfilesDto>> GetUser(Guid publicId)
        {
            try
            {
                var result = await _multiProfileUserService.GetUserWithProfilesAsync(publicId);
                return Ok(result);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpGet("{publicId:guid}/profiles")]
        public async Task<ActionResult<IEnumerable<ProfileInfoDto>>> GetUserProfiles(Guid publicId)
        {
            try
            {
                var result = await _multiProfileUserService.GetUserProfilesAsync(publicId);
                return Ok(result);
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
        }

        [HttpPost("add-profile")]
        public async Task<ActionResult<object>> AddProfile(AddProfileToExistingUserRequest request)
        {
            try
            {
                var profile = await _multiProfileUserService.AddProfileToUserAsync(request);
                return Ok(new { profileId = profile.Id, profileType = profile.ProfileType.ToString() });
            }
            catch (UserNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { error = ex.Message });
            }
        }

        [HttpPost("switch-profile")]
        public async Task<ActionResult<ActiveProfileResponse>> SwitchProfile(SwitchActiveProfileRequest request)
        {
            try
            {
                var result = await _multiProfileUserService.SwitchActiveProfileAsync(request);
                return Ok(result);
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

        [HttpGet("{publicId:guid}/has-profile/{profileType}")]
        public async Task<ActionResult<bool>> HasProfile(Guid publicId, string profileType)
        {
            var type = Enum.Parse<ProfileType>(profileType);
            var result = await _multiProfileUserService.HasProfileAsync(publicId, type);
            return Ok(result);
        }
    }
}