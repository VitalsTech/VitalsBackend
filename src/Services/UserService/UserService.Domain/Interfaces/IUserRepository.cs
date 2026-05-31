using System.Linq.Expressions;
using UserService.Domain.Entities;

namespace UserService.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id);
        Task<User?> GetByPublicIdAsync(Guid publicId);
        Task<User?> GetByPhoneAsync(string phone);
        Task<User?> GetByEmailAsync(string email);
        Task<IEnumerable<User>> GetAllAsync(Expression<Func<User, bool>>? filter = null);
        Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, Expression<Func<User, bool>>? filter = null);
        Task<int> CountAsync(Expression<Func<User, bool>>? filter = null);
        Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null);
        Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null);
        Task AddUserAsync(User user);
        void UpdateUser(User user);
        void DeleteUser(User user);
        Task<Profile?> GetProfileByIdAsync(Guid profileId);
        Task<Profile?> GetProfileByUserAndTypeAsync(Guid userId, ProfileType profileType);
        Task<IEnumerable<Profile>> GetProfilesByUserAsync(Guid userId);
        Task AddProfileAsync(Profile profile);
        void UpdateProfile(Profile profile);
        void DeleteProfile(Profile profile);

        Task<int> SaveChangesAsync();
    }
}