using Microsoft.EntityFrameworkCore;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly AppDbContext _context;

        public RoleRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Role?> GetByIdAsync(Guid id)
            => await _context.Roles.FirstOrDefaultAsync(r => r.Id == id);

        public async Task<Role?> GetByNameAsync(string name)
            => await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);

        public async Task<IEnumerable<Role>> GetAllAsync()
            => await _context.Roles.ToListAsync();

        public async Task<IEnumerable<Role>> GetRolesByOrganizationAsync(Guid organizationId)
            => await _context.Roles.Where(r => r.OrganizationId == organizationId).ToListAsync();

        public async Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId)
            => await _context.UserRoles
                .Where(ur => ur.ProfileId == profileId)
                .Select(ur => ur.Role)
                .ToListAsync();

        public async Task AddAsync(Role role)
            => await _context.Roles.AddAsync(role);

        public void Update(Role role)
            => _context.Roles.Update(role);

        public void Delete(Role role)
            => _context.Roles.Remove(role);

        public async Task<bool> ExistsAsync(string name, Guid? organizationId = null)
        {
            var query = _context.Roles.Where(r => r.Name == name);
            if (organizationId.HasValue)
                query = query.Where(r => r.OrganizationId == organizationId);
            return await query.AnyAsync();
        }
    }
}