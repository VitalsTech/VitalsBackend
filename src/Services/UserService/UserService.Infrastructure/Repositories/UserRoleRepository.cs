using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    public class UserRoleRepository : IUserRoleRepository
    {
        private readonly AppDbContext _context;

        public UserRoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<UserRole?> GetAsync(Guid userId, Guid roleId, Guid profileId)
            => await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId && ur.ProfileId == profileId);

        public async Task<IEnumerable<UserRole>> GetByUserAsync(Guid userId)
            => await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.UserId == userId)
                .ToListAsync();

        public async Task<IEnumerable<UserRole>> GetByProfileAsync(Guid profileId)
            => await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.ProfileId == profileId)
                .ToListAsync();

        public async Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId)
            => await _context.UserRoles
                .Where(ur => ur.ProfileId == profileId)
                .Select(ur => ur.Role)
                .ToListAsync();

        public async Task AddAsync(UserRole userRole)
            => await _context.UserRoles.AddAsync(userRole);

        public void Delete(UserRole userRole)
            => _context.UserRoles.Remove(userRole);

        public async Task<bool> HasRoleAsync(Guid profileId, string roleName)
            => await _context.UserRoles
                .AnyAsync(ur => ur.ProfileId == profileId && ur.Role.Name == roleName);
    }
}