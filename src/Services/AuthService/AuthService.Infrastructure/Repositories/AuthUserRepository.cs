using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public sealed class AuthUserRepository : IAuthUserRepository
{
    private readonly AuthDbContext _db;

    public AuthUserRepository(AuthDbContext db) => _db = db;

    public Task<AuthUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        _db.AuthUsers
            .Include(x => x.EsiaLink)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<AuthUser?> GetByPhoneAsync(string normalizedPhone, CancellationToken cancellationToken = default) =>
        _db.AuthUsers
            .Include(x => x.EsiaLink)
            .FirstOrDefaultAsync(x => x.NormalizedPhone == normalizedPhone, cancellationToken);

    public Task<AuthUser?> GetByUserPublicIdAsync(Guid userPublicId, CancellationToken cancellationToken = default) =>
        _db.AuthUsers
            .Include(x => x.EsiaLink)
            .FirstOrDefaultAsync(x => x.UserPublicId == userPublicId, cancellationToken);

    public Task<AuthUser?> GetByEsiaSubjectIdAsync(string esiaSubjectId, CancellationToken cancellationToken = default) =>
        _db.AuthUsers
            .Include(x => x.EsiaLink)
            .FirstOrDefaultAsync(x => x.EsiaLink != null && x.EsiaLink.EsiaSubjectId == esiaSubjectId, cancellationToken);

    public Task<bool> PhoneExistsAsync(string normalizedPhone, CancellationToken cancellationToken = default) =>
        _db.AuthUsers.AnyAsync(x => x.NormalizedPhone == normalizedPhone, cancellationToken);

    public async Task AddAsync(AuthUser user, CancellationToken cancellationToken = default) =>
        await _db.AuthUsers.AddAsync(user, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
