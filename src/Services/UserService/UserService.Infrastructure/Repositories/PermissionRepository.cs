using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    public class PermissionRepository : IPermissionRepository
    {
        private readonly AppDbContext _context;

        public PermissionRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Permission?> GetByIdAsync(Guid id)
            => await _context.Permissions.FirstOrDefaultAsync(p => p.Id == id);

        public async Task<Permission?> GetByNameAsync(string name)
            => await _context.Permissions.FirstOrDefaultAsync(p => p.Name == name);

        public async Task<IEnumerable<Permission>> GetAllAsync()
            => await _context.Permissions.ToListAsync();

        public async Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(Guid roleId)
            => await _context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => rp.Permission)
                .ToListAsync();

        public async Task<IEnumerable<Permission>> GetPermissionsByProfileAsync(Guid profileId)
            => await _context.UserRoles
                .Where(ur => ur.ProfileId == profileId)
                .SelectMany(ur => ur.Role.RolePermissions)
                .Select(rp => rp.Permission)
                .Distinct()
                .ToListAsync();

        public async Task AddAsync(Permission permission)
            => await _context.Permissions.AddAsync(permission);

        public void Update(Permission permission)
            => _context.Permissions.Update(permission);

        public void Delete(Permission permission)
            => _context.Permissions.Remove(permission);
    }
}