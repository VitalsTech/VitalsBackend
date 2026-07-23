using AutoMapper;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Doctor;
using UserService.Application.DTOs.Patient;
using UserService.Application.Interfaces;
using UserService.Application.Mappings;
using UserService.Application.Services;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using static UserService.UnitTests.TestFactory;

using Profile = UserService.Domain.Entities.Profile;

namespace UserService.UnitTests;

public class MultiProfileUserServiceTests
{
    [Fact]
    public async Task CreateUserWithProfileAsync_creates_user_and_patient_profile()
    {
        var userRepository = new InMemoryUserRepository();
        var service = CreateMultiProfileService(userRepository);

        var request = new CreateUserWithProfileRequest
        {
            PhoneNumber = "89000936941",
            Email = "patient@example.com",
            FirstName = "Ivan",
            SecondName = "Ivanovich",
            Surename = "Petrov",
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Sex = "Male",
            PatientProfile = new CreatePatientProfileRequest
            {
                InsuranceNumber = "1234567890123456",
                SNILS = "12345678901",
                BloodType = "APositive",
                Allergies = "Penicillin"
            }
        };

        var result = await service.CreateUserWithProfileAsync(request);

        Assert.Equal("89000936941", result.PhoneNumber);
        Assert.Equal("Ivan", result.FirstName);
        Assert.Single(result.Profiles);
        Assert.NotNull(result.ActiveProfileId);
        Assert.Equal(result.ActiveProfileId, result.Profiles.Single().ProfileId);

        var profileData = Assert.IsType<PatientProfileData>(result.Profiles.Single().Data);
        Assert.Equal("APositive", profileData.BloodType);
        Assert.Equal("1234567890123456", profileData.InsuranceNumber);
        Assert.Equal("12345678901", profileData.SNILS);
    }

    [Fact]
    public async Task AddProfileToUserAsync_rejects_doctor_profile_via_self_service()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000936941", "doctor@example.com", "Doctor", "House");
        await userRepository.AddUserAsync(user);

        var service = CreateMultiProfileService(userRepository);

        var request = new AddProfileToExistingUserRequest
        {
            UserPublicId = user.PublicId,
            ProfileType = "Doctor",
            DoctorProfile = new CreateDoctorProfileRequest
            {
                Specialization = "Therapist",
                DiplomaNumber = "D-001",
                DiplomaSeries = "AB",
                CertificateNumber = "C-001",
                CertificateExpiryDate = DateTime.UtcNow.AddYears(1),
                Category = "Highest",
                AcademicDegree = "PhD",
                Biography = "10 years practice"
            }
        };

        await Assert.ThrowsAsync<FluentValidation.ValidationException>(() => service.AddProfileToUserAsync(request));
    }

    private static IMultiProfileUserService CreateMultiProfileService(IUserRepository userRepository)
    {
        var mapper = CreateMapper();
        return new MultiProfileUserService(
            userRepository,
            mapper,
            new PassthroughEncryptionService(),
            new InMemoryRoleRepository(),
            new InMemoryUserRoleRepository());
    }
}

public class AdminServiceTests
{
    [Fact]
    public async Task SearchUsersAsync_filters_and_paginates()
    {
        var userRepository = new InMemoryUserRepository();
        var users = new[]
        {
            CreateUser("89000000001", "alpha@example.com", "Ivan", "Petrov"),
            CreateUser("89000000002", "bravo@example.com", "Petr", "Petrov"),
            CreateUser("89000000003", "charlie@example.com", "Sidor", "Ivanov")
        };

        foreach (var user in users)
            await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);

        var result = await service.SearchUsersAsync(new UserSearchRequest
        {
            Surename = "Petrov",
            Page = 1,
            PageSize = 1
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Single(result.Items);
        Assert.Equal("89000000001", result.Items.Single().PhoneNumber);
    }

    [Fact]
    public async Task SetUserStatusAsync_updates_activity_flag()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000000011", "status@example.com", "Status", "User");
        await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);

        var result = await service.SetUserStatusAsync(user.PublicId, new UserStatusRequest
        {
            IsActive = false,
            Reason = "manual"
        });

        Assert.True(result);
        Assert.False(user.IsActive);
        Assert.True(user.UpdatedAt > user.CreatedAt);
    }

    [Fact]
    public async Task BlockUserAsync_sets_block_state()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000000012", "block@example.com", "Block", "User");
        await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);
        var blockedUntil = DateTime.UtcNow.AddDays(2);

        var result = await service.BlockUserAsync(user.PublicId, new UserBlockRequest
        {
            BlockedUntil = blockedUntil,
            BlockReason = "policy"
        });

        Assert.True(result);
        Assert.False(user.IsActive);
        Assert.Equal(blockedUntil, user.BlockedUntil);
        Assert.Equal("policy", user.BlockReason);
    }

    [Fact]
    public async Task UnblockUserAsync_clears_block_state()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000000013", "unblock@example.com", "Unblock", "User");
        user.IsActive = false;
        user.BlockedUntil = DateTime.UtcNow.AddDays(2);
        user.BlockReason = "policy";
        await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);

        var result = await service.UnblockUserAsync(user.PublicId);

        Assert.True(result);
        Assert.True(user.IsActive);
        Assert.Null(user.BlockedUntil);
        Assert.Null(user.BlockReason);
    }

    [Fact]
    public async Task SoftDeleteUserAsync_marks_user_deleted()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000000014", "delete@example.com", "Delete", "User");
        await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);

        var result = await service.SoftDeleteUserAsync(user.PublicId);

        Assert.True(result);
        Assert.False(user.IsActive);
        Assert.Contains("_deleted_", user.Email);
    }

    [Fact]
    public async Task RestoreUserAsync_reactivates_user()
    {
        var userRepository = new InMemoryUserRepository();
        var user = CreateUser("89000000015", "restore@example.com", "Restore", "User");
        user.IsActive = false;
        await userRepository.AddUserAsync(user);

        var service = CreateAdminService(userRepository);

        var result = await service.RestoreUserAsync(user.PublicId);

        Assert.True(result);
        Assert.True(user.IsActive);
    }

    private static IAdminUserService CreateAdminService(IUserRepository userRepository)
    {
        return new AdminService(userRepository, new NullMultiProfileUserService(), CreateMapper());
    }
}

public class PermissionServiceTests
{
    [Fact]
    public async Task GetUserRolesAndPermissionsAsync_returns_distinct_values_for_active_user()
    {
        var userRepository = new InMemoryUserRepository();
        var userRoleRepository = new InMemoryUserRoleRepository();
        var permissionRepository = new InMemoryPermissionRepository();

        var user = CreateUser("89000000021", "perm@example.com", "Perm", "User");
        await userRepository.AddUserAsync(user);

        var profile1 = CreateProfile(user.Id, ProfileType.Doctor);
        var profile2 = CreateProfile(user.Id, ProfileType.Organization);
        profile2.IsActive = false;
        await userRepository.AddProfileAsync(profile1);
        await userRepository.AddProfileAsync(profile2);

        var adminRole = CreateRole("Admin");
        var viewerRole = CreateRole("Viewer");

        await userRoleRepository.AddAsync(CreateUserRole(user.Id, profile1.Id, adminRole));
        await userRoleRepository.AddAsync(CreateUserRole(user.Id, profile2.Id, viewerRole));
        await userRoleRepository.AddAsync(CreateUserRole(user.Id, profile2.Id, adminRole));

        permissionRepository.SetPermissions(profile1.Id, new[]
        {
            CreatePermission("users.read"),
            CreatePermission("users.block")
        });
        permissionRepository.SetPermissions(profile2.Id, new[]
        {
            CreatePermission("users.block"),
            CreatePermission("users.write")
        });

        var service = new PermissionService(userRepository, userRoleRepository, permissionRepository);

        var response = await service.GetUserRolesAndPermissionsAsync(user.PublicId);

        Assert.Equal(user.PublicId, response.UserPublicId);
        Assert.Equal(new[] { "Admin" }, response.Roles.OrderBy(x => x).ToArray());
        Assert.Equal(new[] { "users.block", "users.read" }, response.Permissions.OrderBy(x => x).ToArray());
        Assert.True(await service.HasRoleAsync(user.PublicId, "Admin"));
        Assert.True(await service.HasPermissionAsync(user.PublicId, "users.write"));
    }

    [Fact]
    public async Task Blocked_user_is_denied_roles_permissions_and_check_permission()
    {
        var userRepository = new InMemoryUserRepository();
        var userRoleRepository = new InMemoryUserRoleRepository();
        var permissionRepository = new InMemoryPermissionRepository();

        var user = CreateUser("89000000022", "blocked@example.com", "Blocked", "User");
        user.BlockedUntil = DateTime.UtcNow.AddHours(1);
        await userRepository.AddUserAsync(user);

        var profile = CreateProfile(user.Id, ProfileType.Doctor);
        await userRepository.AddProfileAsync(profile);
        await userRoleRepository.AddAsync(CreateUserRole(user.Id, profile.Id, CreateRole("Admin")));
        permissionRepository.SetPermissions(profile.Id, new[] { CreatePermission("users.block") });

        var service = new PermissionService(userRepository, userRoleRepository, permissionRepository);

        Assert.False(await service.HasRoleAsync(user.PublicId, "Admin"));
        Assert.False(await service.HasPermissionAsync(user.PublicId, "users.block"));

        var check = await service.CheckPermissionAsync(new PermissionCheckRequest
        {
            UserPublicId = user.PublicId,
            Permission = "users.block"
        });

        Assert.False(check.HasPermission);
        Assert.Equal("User is blocked or inactive", check.Reason);
    }
}

// ============================================================
// ВСПОМОГАТЕЛЬНЫЕ КЛАССЫ И МЕТОДЫ
// ============================================================

internal static class TestFactory
{
    internal static IMapper CreateMapper()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>());
        return config.CreateMapper();
    }

    internal static User CreateUser(string phoneNumber, string email, string firstName, string surename)
    {
        return new User
        {
            PhoneNumber = phoneNumber,
            Email = email,
            FirstName = firstName,
            Surename = surename,
            BirthDate = new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Sex = Sex.Male
        };
    }

    internal static Profile CreateProfile(Guid userId, ProfileType profileType)
    {
        return new Profile
        {
            UserId = userId,
            ProfileType = profileType,
            IsActive = true
        };
    }

    internal static Role CreateRole(string name)
    {
        return new Role { Id = Guid.NewGuid(), Name = name };
    }

    internal static Permission CreatePermission(string name)
    {
        return new Permission { Id = Guid.NewGuid(), Name = name };
    }

    internal static UserRole CreateUserRole(Guid userId, Guid profileId, Role role)
    {
        return new UserRole
        {
            UserId = userId,
            ProfileId = profileId,
            RoleId = role.Id,
            Role = role,
            AssignedAt = DateTime.UtcNow
        };
    }
}

internal sealed class PassthroughEncryptionService : IEncryptionService
{
    public string Encrypt(string plainText) => plainText;
    public string Decrypt(string cipherText) => cipherText;
}

internal sealed class NullMultiProfileUserService : IMultiProfileUserService
{
    public Task<UserWithProfilesDto> CreateUserWithProfileAsync(CreateUserWithProfileRequest request)
        => throw new NotSupportedException();

    public Task<UserWithProfilesDto> GetUserWithProfilesAsync(Guid publicId)
        => throw new NotSupportedException();

    public Task<Profile> AddProfileToUserAsync(AddProfileToExistingUserRequest request)
        => throw new NotSupportedException();

    public Task<ActiveProfileResponse> SwitchActiveProfileAsync(SwitchActiveProfileRequest request)
        => throw new NotSupportedException();

    public Task<IEnumerable<ProfileInfoDto>> GetUserProfilesAsync(Guid userPublicId)
        => throw new NotSupportedException();

    public Task<bool> HasProfileAsync(Guid userPublicId, ProfileType profileType)
        => throw new NotSupportedException();

    public Task<UserWithProfilesDto?> GetUserByPhoneAsync(string phone)
        => throw new NotSupportedException();

    public Task<UserWithProfilesDto?> GetUserByEmailAsync(string email)
        => throw new NotSupportedException();
}

internal sealed class InMemoryUserRepository : IUserRepository
{
    private readonly List<User> _users = new();
    private readonly List<Profile> _profiles = new();

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByPublicIdAsync(Guid publicId)
        => Task.FromResult(_users.FirstOrDefault(u => u.PublicId == publicId));

    public Task<User?> GetByPhoneAsync(string phone)
        => Task.FromResult(_users.FirstOrDefault(u => u.PhoneNumber == phone));

    public Task<User?> GetByEmailAsync(string email)
        => Task.FromResult(_users.FirstOrDefault(u => u.Email == email));

    public Task<IEnumerable<User>> GetAllAsync(System.Linq.Expressions.Expression<Func<User, bool>>? filter = null)
        => Task.FromResult(Filter(_users, filter));

    public Task<IEnumerable<User>> GetPagedAsync(int page, int pageSize, System.Linq.Expressions.Expression<Func<User, bool>>? filter = null)
    {
        var query = _users.AsQueryable();
        if (filter != null)
            query = query.Where(filter);
        var result = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return Task.FromResult(result.AsEnumerable());
    }

    public Task<int> CountAsync(System.Linq.Expressions.Expression<Func<User, bool>>? filter = null)
        => Task.FromResult(Filter(_users, filter).Count());

    public Task<bool> IsPhoneUniqueAsync(string phone, Guid? excludeId = null)
        => Task.FromResult(!_users.Any(u => u.PhoneNumber == phone && (!excludeId.HasValue || u.Id != excludeId.Value)));

    public Task<bool> IsEmailUniqueAsync(string email, Guid? excludeId = null)
        => Task.FromResult(string.IsNullOrEmpty(email) || !_users.Any(u => u.Email == email && (!excludeId.HasValue || u.Id != excludeId.Value)));

    public Task AddUserAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public void UpdateUser(User user) { }

    public void DeleteUser(User user) => _users.Remove(user);

    public Task<Profile?> GetProfileByIdAsync(Guid profileId)
        => Task.FromResult(_profiles.FirstOrDefault(p => p.Id == profileId));

    public Task<Profile?> GetProfileByUserAndTypeAsync(Guid userId, ProfileType profileType)
        => Task.FromResult(_profiles.FirstOrDefault(p => p.UserId == userId && p.ProfileType == profileType));

    public Task<IEnumerable<Profile>> GetProfilesByUserAsync(Guid userId)
        => Task.FromResult(_profiles.Where(p => p.UserId == userId).AsEnumerable());

    public Task AddProfileAsync(Profile profile)
    {
        _profiles.Add(profile);
        return Task.CompletedTask;
    }

    public void UpdateProfile(Profile profile) { }

    public void DeleteProfile(Profile profile) => _profiles.Remove(profile);

    public Task<int> SaveChangesAsync() => Task.FromResult(1);

    private static IEnumerable<User> Filter(IEnumerable<User> source, System.Linq.Expressions.Expression<Func<User, bool>>? filter)
    {
        IQueryable<User> query = source.AsQueryable();
        if (filter != null)
            query = query.Where(filter);
        return query.ToList();
    }
}

internal sealed class InMemoryRoleRepository : IRoleRepository
{
    private readonly List<Role> _roles =
    [
        new() { Id = Guid.NewGuid(), Name = "Patient" },
        new() { Id = Guid.NewGuid(), Name = "Doctor" },
        new() { Id = Guid.NewGuid(), Name = "ClinicAdmin" }
    ];

    public Task<Role?> GetByIdAsync(Guid id)
        => Task.FromResult(_roles.FirstOrDefault(r => r.Id == id));

    public Task<Role?> GetByNameAsync(string name)
        => Task.FromResult(_roles.FirstOrDefault(r => r.Name == name));

    public Task<IEnumerable<Role>> GetAllAsync()
        => Task.FromResult(_roles.AsEnumerable());

    public Task<IEnumerable<Role>> GetRolesByOrganizationAsync(Guid organizationId)
        => Task.FromResult(Enumerable.Empty<Role>());

    public Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId)
        => Task.FromResult(Enumerable.Empty<Role>());

    public Task AddAsync(Role role)
    {
        _roles.Add(role);
        return Task.CompletedTask;
    }

    public void Update(Role role) { }

    public void Delete(Role role) => _roles.Remove(role);

    public Task<bool> ExistsAsync(string name, Guid? organizationId = null)
        => Task.FromResult(_roles.Any(r => r.Name == name));
}

internal sealed class InMemoryUserRoleRepository : IUserRoleRepository
{
    private readonly List<UserRole> _items = new();

    public Task<UserRole?> GetAsync(Guid userId, Guid roleId, Guid profileId)
        => Task.FromResult(_items.FirstOrDefault(x => x.UserId == userId && x.RoleId == roleId && x.ProfileId == profileId));

    public Task<IEnumerable<UserRole>> GetByUserAsync(Guid userId)
        => Task.FromResult(_items.Where(x => x.UserId == userId).AsEnumerable());

    public Task<IEnumerable<UserRole>> GetByProfileAsync(Guid profileId)
        => Task.FromResult(_items.Where(x => x.ProfileId == profileId).AsEnumerable());

    public Task<IEnumerable<Role>> GetRolesByProfileAsync(Guid profileId)
        => Task.FromResult(_items.Where(x => x.ProfileId == profileId && x.Role != null).Select(x => x.Role).DistinctBy(r => r.Id).AsEnumerable());

    public Task AddAsync(UserRole userRole)
    {
        _items.Add(userRole);
        return Task.CompletedTask;
    }

    public void Delete(UserRole userRole) => _items.Remove(userRole);

    public Task<bool> HasRoleAsync(Guid profileId, string roleName)
        => Task.FromResult(_items.Any(x => x.ProfileId == profileId && x.Role != null && x.Role.Name == roleName));
}

internal sealed class InMemoryPermissionRepository : IPermissionRepository
{
    private readonly List<Permission> _permissions = new();
    private readonly Dictionary<Guid, List<Permission>> _byProfile = new();

    public void SetPermissions(Guid profileId, IEnumerable<Permission> permissions)
    {
        var list = permissions.ToList();
        _byProfile[profileId] = list;
        foreach (var permission in list)
        {
            if (_permissions.All(x => x.Id != permission.Id))
                _permissions.Add(permission);
        }
    }

    public Task<Permission?> GetByIdAsync(Guid id)
        => Task.FromResult(_permissions.FirstOrDefault(x => x.Id == id));

    public Task<Permission?> GetByNameAsync(string name)
        => Task.FromResult(_permissions.FirstOrDefault(x => x.Name == name));

    public Task<IEnumerable<Permission>> GetAllAsync()
        => Task.FromResult(_permissions.AsEnumerable());

    public Task<IEnumerable<Permission>> GetPermissionsByRoleAsync(Guid roleId)
        => Task.FromResult(Array.Empty<Permission>().AsEnumerable());

    public Task<IEnumerable<Permission>> GetPermissionsByProfileAsync(Guid profileId)
        => Task.FromResult(_byProfile.TryGetValue(profileId, out var permissions) ? permissions.AsEnumerable() : Enumerable.Empty<Permission>());

    public Task AddAsync(Permission permission)
    {
        _permissions.Add(permission);
        return Task.CompletedTask;
    }

    public void Update(Permission permission) { }

    public void Delete(Permission permission) => _permissions.Remove(permission);
}