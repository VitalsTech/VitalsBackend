using AuthService.Domain.Entities;
using AuthService.Domain.Interfaces;
using AuthService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Infrastructure.Repositories;

public sealed class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly AuthDbContext _db;

    public PasswordResetTokenRepository(AuthDbContext db) => _db = db;

    public Task<PasswordResetToken?> GetActiveByCodeAsync(
        Guid authUserId,
        string code,
        CancellationToken cancellationToken = default) =>
        _db.PasswordResetTokens.FirstOrDefaultAsync(
            x => x.AuthUserId == authUserId &&
                 x.Code == code &&
                 !x.IsUsed &&
                 x.ExpiresAt > DateTime.UtcNow,
            cancellationToken);

    public async Task AddAsync(PasswordResetToken token, CancellationToken cancellationToken = default) =>
        await _db.PasswordResetTokens.AddAsync(token, cancellationToken);

    public async Task InvalidateActiveForUserAsync(Guid authUserId, CancellationToken cancellationToken = default)
    {
        await _db.PasswordResetTokens
            .Where(x => x.AuthUserId == authUserId && !x.IsUsed)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.IsUsed, true), cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        _db.SaveChangesAsync(cancellationToken);
}
