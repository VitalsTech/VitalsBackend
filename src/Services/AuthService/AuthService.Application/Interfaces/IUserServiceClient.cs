using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IUserServiceClient
{
    Task<UserServiceUserDto> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserServiceUserDto?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<UserServiceRoleResponse> GetRolesAndPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken = default);
    Task SwitchActiveProfileAsync(Guid userPublicId, Guid profileId, CancellationToken cancellationToken = default);
    Task ActivateProfileByTypeAsync(Guid userPublicId, string profileType, CancellationToken cancellationToken = default);
    Task ApplyEsiaProfileAsync(Guid userPublicId, EsiaUserInfo esiaUser, CancellationToken cancellationToken = default);
}
