using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using UserService.Domain.Entities;
using UserService.Domain.Interfaces;
using UserService.Infrastructure.Data;

namespace UserService.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _context;

        public UserRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<User?> GetByIdAsync(Guid id)
            => await _context.Users.FirstOrDefaultAsync(u => u.Id == id);

        public async Task<User?> GetByPublicIdAsync(Guid publicId)
            => await _context.Users.FirstOrDefaultAsync(u => u.PublicId == publicId);

        public async Task<User?> GetByPhoneAsync(string phone)
            => await _context.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);

        public async Task<User?> GetByEmailAsync(string email)
            => string.IsNullOrEmpty(email) ? null : await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

        public async Task<IEnumerable<User>> GetAllAsync(Expression<Func<User, bool>>? filter = null)
        {
            IQueryable<User> query = _context.Users;
            if (filter != null)
                query = query.Where(filter);
            return await query.ToListAsync();
        }

        public async Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, Expression<Func<User, bool>>? filter = null)
        {
            IQueryable<User> query = _context.Users;
            if (filter != null)
                query = query.Where(filter);
            return await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<int> CountAsync(Expression<Func<User, bool>>? filter = null)
        {
            IQueryable<User> query = _context.Users;
            if (filter != null)
                query = query.Where(filter);
            return await query.CountAsync();
        }

        public async Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null)
        {
            var query = _context.Users.Where(u => u.PhoneNumber == phone);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            return !await query.AnyAsync();
        }

        public async Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        {
            if (string.IsNullOrEmpty(email))
                return true;
            var query = _context.Users.Where(u => u.Email == email);
            if (excludeId.HasValue)
                query = query.Where(u => u.Id != excludeId.Value);
            return !await query.AnyAsync();
        }

        public async Task AddUserAsync(User user)
            => await _context.Users.AddAsync(user);

        public void UpdateUser(User user)
            => _context.Users.Update(user);

        public void DeleteUser(User user)
            => _context.Users.Remove(user);

        public async Task<Profile?> GetProfileByIdAsync(Guid profileId)
            => await _context.Profiles
                .Include(p => p.PatientProfile)
                .Include(p => p.DoctorProfile)
                .Include(p => p.OrganizationProfile)
                .FirstOrDefaultAsync(p => p.Id == profileId);

        public async Task<Profile?> GetProfileByUserAndTypeAsync(Guid userId, ProfileType profileType)
            => await _context.Profiles
                .Include(p => p.PatientProfile)
                .Include(p => p.DoctorProfile)
                .Include(p => p.OrganizationProfile)
                .FirstOrDefaultAsync(p => p.UserId == userId && p.ProfileType == profileType);

        public async Task<IEnumerable<Profile>> GetProfilesByUserAsync(Guid userId)
            => await _context.Profiles
                .Include(p => p.PatientProfile)
                .Include(p => p.DoctorProfile)
                .Include(p => p.OrganizationProfile)
                .Where(p => p.UserId == userId)
                .ToListAsync();

        public async Task AddProfileAsync(Profile profile)
            => await _context.Profiles.AddAsync(profile);

        public void UpdateProfile(Profile profile)
            => _context.Profiles.Update(profile);

        public void DeleteProfile(Profile profile)
            => _context.Profiles.Remove(profile);

        public async Task<int> SaveChangesAsync()
            => await _context.SaveChangesAsync();
    }
}