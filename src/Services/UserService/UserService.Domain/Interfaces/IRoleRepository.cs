using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        Task<IEnumerable<Role>> GetAllAsync();
        Task<IEnumerable<Role>> GetRolesByOrganizationAsync(Guid organizationId);
        Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId);
        Task AddAsync(Role role);
        void Update(Role role);
        void Delete(Role role);
        Task<bool> ExistsAsync(string name, Guid? organizationId = null);
    }
}