using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces
{
    public interface IUserRoleRepository
    {
        Task<UserRole?> GetAsync(Guid userId, Guid roleId, Guid profileId);
        Task<IEnumerable<UserRole>> GetByUserAsync(Guid userId);
        Task<IEnumerable<UserRole>> GetByProfileAsync(Guid profileId);
        Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId);
        Task AddAsync(UserRole userRole);
        void Delete(UserRole userRole);
        Task<bool> HasRoleAsync(Guid profileId, string roleName);
    }
}