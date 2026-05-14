using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces
{
    public interface IPermissionRepository
    {
        Task<Permission?> GetByIdAsync(Guid id);
        Task<Permission?> GetByNameAsync(string name);
        Task<IEnumerable<Permission>> GetAllAsync();
        Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(Guid roleId);
        Task<IEnumerable<Permission>> GetPermissionsByProfileAsync(Guid profileId);
        Task AddAsync(Permission permission);
        void Update(Permission permission);
        void Delete(Permission permission);
    }
}