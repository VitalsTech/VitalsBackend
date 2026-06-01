using AuthService.Domain.Entities;

namespace AuthService.Domain.Interfaces;

public interface IAuthUserRepository
{
    Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetByUserPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken = default);
    Task<AuthUser?> GetByEsiaSubjectIdAsync(string esiaSubjectId, CancellationToken cancellationToken = default);
    Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken cancellationToken = default);
    Task AddAsync(AuthUser user, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
