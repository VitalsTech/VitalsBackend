using AuthService.Application.DTOs;

namespace AuthService.Application.Interfaces;

public interface IUserServiceClient
{
    Task<UserServiceUserDto> RegisterUserAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<UserServiceUserDto?> GetUserByPhoneAsync(string phone, CancellationToken cancellationToken = default);
    Task<UserServiceRoleResponse> GetRolesAndPermissionsAsync(Guid userPublicId, CancellationToken cancellationToken = default);
}
