using AutoMapper;
using System.Net;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Doctor;
using UserService.Application.DTOs.Organization;
using UserService.Application.DTOs.Patient;
using UserService.Application.Exceptions;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Enums;
using UserService.Domain.Interfaces;
using FluentValidation;
using Profile = UserService.Domain.Entities.Profile;

namespace UserService.Application.Services
{
    public interface IMultiProfileUserService
    {
        Task<UserWithProfilesDto> CreateUserWithProfileAsync(CreateUserWithProfileRequest request);
        Task<UserWithProfilesDto> GetUserWithProfilesAsync(Guid publicId);
        Task<Profile> AddProfileToUserAsync(AddProfileToExistingUserRequest request);
        Task<ActiveProfileResponse> SwitchActiveProfileAsync(SwitchActiveProfileRequest request);
        Task<IEnumerable<ProfileInfoDto>> GetUserProfilesAsync(Guid userPublicId);
        Task<bool> HasProfileAsync(Guid userPublicId, ProfileType profileType);
        Task<UserWithProfilesDto?> GetUserByPhoneAsync(string phone);
        Task<UserWithProfilesDto?> GetUserByEmailAsync(string email);
        Task<UserWithProfilesDto> UpdateDoctorProfileAsync(Guid userPublicId, UpdateDoctorProfileRequest request);
        Task<UserWithProfilesDto> ApplyEsiaProfileAsync(Guid userPublicId, ApplyEsiaProfileRequest request);

        /// <summary>
        /// Resolve PublicId or ProfileId → user. Used when services store either identity.
        /// </summary>
        Task<UserWithProfilesDto?> GetUserByPublicIdOrProfileIdAsync(Guid id);

        /// <summary>
        /// PublicId ∪ all profile ids for the user (always includes the input id).
        /// </summary>
        Task<IReadOnlyList<Guid>> ResolveIdentityIdsAsync(Guid id);
    }

    public class MultiProfileUserService : IMultiProfileUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IEncryptionService _encryptionService;
        private readonly IRoleRepository _roleRepository;
        private readonly IUserRoleRepository _userRoleRepository;

        public MultiProfileUserService(
            IUserRepository userRepository,
            IMapper mapper,
            IEncryptionService encryptionService,
            IRoleRepository roleRepository,
            IUserRoleRepository userRoleRepository)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _encryptionService = encryptionService;
            _roleRepository = roleRepository;
            _userRoleRepository = userRoleRepository;
        }

        private static string? DefaultRoleNameFor(ProfileType profileType) => profileType switch
        {
            ProfileType.Patient => "Patient",
            ProfileType.Doctor => "Doctor",
            ProfileType.Organization => "ClinicAdmin",
            _ => null
        };

        private async Task AssignDefaultRoleAsync(Guid userId, Profile profile)
        {
            var roleName = DefaultRoleNameFor(profile.ProfileType);
            if (roleName is null)
                return;

            if (await _userRoleRepository.HasRoleAsync(profile.Id, roleName))
                return;

            var role = await _roleRepository.GetByNameAsync(roleName);
            if (role is null)
                return;

            await _userRoleRepository.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = role.Id,
                ProfileId = profile.Id
            });
        }

        public async Task<UserWithProfilesDto> CreateUserWithProfileAsync(CreateUserWithProfileRequest request)
        {
            // Проверка уникальности телефона
            if (!await _userRepository.IsPhoneUniqueAsync(request.PhoneNumber))
                throw new DuplicatePhoneException($"Phone {request.PhoneNumber} already exists");
            if (!string.IsNullOrEmpty(request.Email) && !await _userRepository.IsEmailUniqueAsync(request.Email))
                throw new DuplicateEmailException($"Email {request.Email} already exists");

            // Создание пользователя
            var user = new User
            {
                PhoneNumber = request.PhoneNumber,
                Email = request.Email,
                FirstName = request.FirstName,
                SecondName = request.SecondName,
                Surename = request.Surename,
                BirthDate = request.BirthDate,
                Sex = Enum.Parse<Sex>(request.Sex)
            };

            await _userRepository.AddUserAsync(user);
            await _userRepository.SaveChangesAsync();

            // Добавление профиля
            Profile profile = null!;

            if (request.PatientProfile != null)
            {
                profile = await CreatePatientProfile(user.Id, request.PatientProfile);
            }
            else if (request.DoctorProfile != null)
            {
                profile = await CreateDoctorProfile(user.Id, request.DoctorProfile);
            }
            else if (request.OrganizationProfile != null)
            {
                profile = await CreateOrganizationProfile(user.Id, request.OrganizationProfile);
            }
            else
            {
                throw new ArgumentException("At least one profile must be provided");
            }

            profile.IsActive = true;
            await _userRepository.AddProfileAsync(profile);
            await _userRepository.SaveChangesAsync();

            await AssignDefaultRoleAsync(user.Id, profile);
            await _userRepository.SaveChangesAsync();

            return await GetUserWithProfilesAsync(user.PublicId);
        }

        public async Task<UserWithProfilesDto> GetUserWithProfilesAsync(Guid publicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(publicId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {publicId} not found");

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);

            var result = new UserWithProfilesDto
            {
                PublicId = user.PublicId,
                PhoneNumber = user.PhoneNumber,
                Email = user.Email,
                FirstName = user.FirstName,
                SecondName = user.SecondName,
                Surename = user.Surename,
                BirthDate = user.BirthDate,
                Sex = user.Sex.ToString(),
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Profiles = profiles.Select(p => new ProfileInfoDto
                {
                    ProfileId = p.Id,
                    ProfileType = p.ProfileType.ToString(),
                    IsActive = p.IsActive,
                    Data = GetProfileData(p)
                }).ToList(),
                ActiveProfileId = profiles.FirstOrDefault(p => p.IsActive)?.Id
            };

            return result;
        }

        public async Task<Profile> AddProfileToUserAsync(AddProfileToExistingUserRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {request.UserPublicId} not found");

            if (!Enum.TryParse<ProfileType>(request.ProfileType, true, out var profileType))
            {
                throw new ValidationException($"Invalid profile type value. Allowed values: Patient, Doctor, Organization. Received: {request.ProfileType}");
            }

            // Self-service add-profile may only create Patient. Doctor/Organization require registration or admin flow.
            if (profileType is ProfileType.Doctor or ProfileType.Organization)
            {
                throw new ValidationException(
                    "Doctor and Organization profiles cannot be added via self-service. Register with the desired profile or use an admin API.");
            }

            var existingProfile = await _userRepository.GetProfileByUserAndTypeAsync(user.Id, profileType);
            if (existingProfile != null)
                throw new InvalidOperationException($"User already has a {request.ProfileType} profile");

            Profile profile = null!;

            profile = profileType switch
            {
                ProfileType.Patient when request.PatientProfile != null => await CreatePatientProfile(user.Id, request.PatientProfile),
                _ => throw new ValidationException($"{profileType} profile data must be provided")
            };

            await _userRepository.AddProfileAsync(profile);
            await _userRepository.SaveChangesAsync();

            await AssignDefaultRoleAsync(user.Id, profile);
            await _userRepository.SaveChangesAsync();

            return profile;
        }

        public async Task<ActiveProfileResponse> SwitchActiveProfileAsync(SwitchActiveProfileRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(request.UserPublicId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {request.UserPublicId} not found");

            // Деактивируем все профили пользователя
            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);
            foreach (var profile in profiles)
            {
                profile.IsActive = false;
                _userRepository.UpdateProfile(profile);
            }

            // Активируем выбранный профиль
            var targetProfile = profiles.FirstOrDefault(p => p.Id == request.ProfileId);
            if (targetProfile == null)
                throw new ArgumentException("Profile not found for this user");

            targetProfile.IsActive = true;
            _userRepository.UpdateProfile(targetProfile);
            await _userRepository.SaveChangesAsync();

            return new ActiveProfileResponse
            {
                ProfileId = targetProfile.Id,
                ProfileType = targetProfile.ProfileType.ToString(),
                ProfileData = GetProfileData(targetProfile)
            };
        }

        public async Task<IEnumerable<ProfileInfoDto>> GetUserProfilesAsync(Guid userPublicId)
        {
            var user = await _userRepository.GetByPublicIdAsync(userPublicId);
            if (user == null)
                throw new UserNotFoundException($"User with ID {userPublicId} not found");

            var profiles = await _userRepository.GetProfilesByUserAsync(user.Id);

            return profiles.Select(p => new ProfileInfoDto
            {
                ProfileId = p.Id,
                ProfileType = p.ProfileType.ToString(),
                IsActive = p.IsActive,
                Data = GetProfileData(p)
            });
        }

        public async Task<bool> HasProfileAsync(Guid userPublicId, ProfileType profileType)
        {
            var user = await _userRepository.GetByPublicIdAsync(userPublicId);
            if (user == null)
                return false;

            var profile = await _userRepository.GetProfileByUserAndTypeAsync(user.Id, profileType);
            return profile != null;
        }

        private async Task<Profile> CreatePatientProfile(Guid userId, CreatePatientProfileRequest request)
        {
            var profile = new Profile
            {
                UserId = userId,
                ProfileType = ProfileType.Patient
            };

            var patientProfile = new PatientProfile
            {
                Id = profile.Id,
                BloodType = ParseOptionalEnum<BloodType>(request.BloodType, "blood type"),
                Allergies = request.Allergies,
                ResidenceAddress = MapAddress(request.ResidenceAddress),
                RegistrationAddress = MapAddress(request.RegistrationAddress)
            };

            if (!string.IsNullOrEmpty(request.InsuranceNumber))
                patientProfile.InsuranceNumber = _encryptionService.Encrypt(request.InsuranceNumber);
            if (!string.IsNullOrEmpty(request.SNILS))
                patientProfile.SNILS = _encryptionService.Encrypt(request.SNILS);

            profile.PatientProfile = patientProfile;
            return profile;
        }

        private async Task<Profile> CreateDoctorProfile(Guid userId, CreateDoctorProfileRequest request)
        {
            if (!Enum.TryParse<DoctorCategory>(request.Category, true, out var category))
            {
                throw new ValidationException($"Invalid category value. Allowed values: None, Second, First, Highest. Received: {request.Category}");
            }
            var profile = new Profile
            {
                UserId = userId,
                ProfileType = ProfileType.Doctor
            };

            var doctorProfile = new DoctorProfile
            {
                Id = profile.Id,
                Specialization = request.Specialization,
                DiplomaNumber = request.DiplomaNumber,
                DiplomaSeries = request.DiplomaSeries,
                CertificateNumber = request.CertificateNumber,
                CertificateExpiryDate = request.CertificateExpiryDate,
                OrganizationId = request.OrganizationId,
                Category = category,
                AcademicDegree = request.AcademicDegree,
                Biography = request.Biography
            };

            profile.DoctorProfile = doctorProfile;
            return profile;
        }

        private async Task<Profile> CreateOrganizationProfile(Guid userId, CreateOrganizationProfileRequest request)
        {
            var profile = new Profile
            {
                UserId = userId,
                ProfileType = ProfileType.Organization
            };

            var orgProfile = new OrganizationProfile
            {
                Id = profile.Id,
                LegalName = request.LegalName,
                DisplayName = request.DisplayName,
                INN = request.INN,
                KPP = request.KPP,
                OGRN = request.OGRN,
                Role = ParseRequiredEnum<OrganizationRole>(request.Role, "role"),
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                AdministratorId = request.AdministratorId
            };

            if (request.LegalAddress != null)
            {
                orgProfile.LegalAddress = new Address
                {
                    PostCode = request.LegalAddress.PostCode,
                    Country = request.LegalAddress.Country,
                    Region = request.LegalAddress.Region,
                    City = request.LegalAddress.City,
                    Area = request.LegalAddress.Area,
                    Street = request.LegalAddress.Street,
                    House = request.LegalAddress.House,
                    Flat = request.LegalAddress.Flat
                };
            }

            profile.OrganizationProfile = orgProfile;
            return profile;
        }

        private static TEnum ParseRequiredEnum<TEnum>(string value, string fieldName)
            where TEnum : struct, Enum
        {
            if (Enum.TryParse<TEnum>(value, true, out var parsed))
                return parsed;

            var allowedValues = string.Join(", ", Enum.GetNames<TEnum>());
            throw new ValidationException($"Invalid {fieldName} value. Allowed values: {allowedValues}. Received: {value}");
        }

        private static TEnum? ParseOptionalEnum<TEnum>(string? value, string fieldName)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return ParseRequiredEnum<TEnum>(value, fieldName);
        }

        private object GetProfileData(Profile profile)
        {
            if (profile.PatientProfile != null)
            {
                return new PatientProfileData
                {
                    InsuranceNumber = DecryptIfNeeded(profile.PatientProfile.InsuranceNumber),
                    SNILS = DecryptIfNeeded(profile.PatientProfile.SNILS),
                    BloodType = profile.PatientProfile.BloodType?.ToString(),
                    Allergies = profile.PatientProfile.Allergies,
                    ResidenceAddress = MapAddressDto(profile.PatientProfile.ResidenceAddress),
                    RegistrationAddress = MapAddressDto(profile.PatientProfile.RegistrationAddress),
                    DoctorIds = profile.PatientProfile.DoctorIds,
                    OrganizationIds = profile.PatientProfile.OrganizationIds
                };
            }

            if (profile.DoctorProfile != null)
            {
                return new DoctorProfileData
                {
                    Specialization = profile.DoctorProfile.Specialization,
                    DiplomaNumber = profile.DoctorProfile.DiplomaNumber,
                    DiplomaSeries = profile.DoctorProfile.DiplomaSeries,
                    CertificateNumber = profile.DoctorProfile.CertificateNumber,
                    CertificateExpiryDate = profile.DoctorProfile.CertificateExpiryDate,
                    Category = profile.DoctorProfile.Category.ToString(),
                    AcademicDegree = profile.DoctorProfile.AcademicDegree,
                    Biography = profile.DoctorProfile.Biography,
                    Rating = profile.DoctorProfile.Rating,
                    IsCertificateValid = profile.DoctorProfile.IsCertificateValid()
                };
            }

            if (profile.OrganizationProfile != null)
            {
                return new OrganizationProfileData
                {
                    LegalName = profile.OrganizationProfile.LegalName,
                    DisplayName = profile.OrganizationProfile.DisplayName,
                    INN = profile.OrganizationProfile.INN,
                    KPP = profile.OrganizationProfile.KPP,
                    OGRN = profile.OrganizationProfile.OGRN,
                    Role = profile.OrganizationProfile.Role.ToString(),
                    ContactPhone = profile.OrganizationProfile.ContactPhone,
                    ContactEmail = profile.OrganizationProfile.ContactEmail
                };
            }

            return new { };
        }
        public async Task<UserWithProfilesDto?> GetUserByPhoneAsync(string phone)
        {
            var user = await _userRepository.GetByPhoneAsync(phone);
            if (user == null)
                return null;

            return await GetUserWithProfilesAsync(user.PublicId);
        }

        public async Task<UserWithProfilesDto?> GetUserByEmailAsync(string email)
        {
            if (string.IsNullOrEmpty(email))
                return null;

            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
                return null;

            return await GetUserWithProfilesAsync(user.PublicId);
        }

        public async Task<UserWithProfilesDto> UpdateDoctorProfileAsync(Guid userPublicId, UpdateDoctorProfileRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(userPublicId)
                ?? throw new UserNotFoundException($"User {userPublicId} not found.");

            var profile = await _userRepository.GetProfileByUserAndTypeAsync(user.Id, ProfileType.Doctor)
                ?? throw new InvalidOperationException("У пользователя нет профиля врача.");

            if (!profile.IsActive)
                throw new InvalidOperationException("Профиль врача неактивен.");

            if (profile.DoctorProfile is null)
                throw new InvalidOperationException("Профиль врача не найден.");

            if (!string.IsNullOrWhiteSpace(request.Specialization))
                profile.DoctorProfile.Specialization = request.Specialization.Trim();

            if (request.Biography is not null)
                profile.DoctorProfile.Biography = string.IsNullOrWhiteSpace(request.Biography) ? null : request.Biography.Trim();

            if (request.AcademicDegree is not null)
                profile.DoctorProfile.AcademicDegree = string.IsNullOrWhiteSpace(request.AcademicDegree) ? null : request.AcademicDegree.Trim();

            _userRepository.UpdateProfile(profile);
            await _userRepository.SaveChangesAsync();
            return await GetUserWithProfilesAsync(userPublicId);
        }

        public async Task<UserWithProfilesDto> ApplyEsiaProfileAsync(Guid userPublicId, ApplyEsiaProfileRequest request)
        {
            var user = await _userRepository.GetByPublicIdAsync(userPublicId)
                ?? throw new UserNotFoundException($"User {userPublicId} not found.");

            if (!string.IsNullOrWhiteSpace(request.FirstName))
                user.FirstName = request.FirstName.Trim();
            if (request.SecondName is not null)
                user.SecondName = string.IsNullOrWhiteSpace(request.SecondName) ? null : request.SecondName.Trim();
            if (!string.IsNullOrWhiteSpace(request.Surename))
                user.Surename = request.Surename.Trim();
            if (request.BirthDate is { } birthDate && birthDate.Year > 1900)
                user.BirthDate = DateTime.SpecifyKind(birthDate.Date, DateTimeKind.Utc);
            if (!string.IsNullOrWhiteSpace(request.Sex) && Enum.TryParse<Sex>(NormalizeSex(request.Sex), true, out var sex))
                user.Sex = sex;
            if (!string.IsNullOrWhiteSpace(request.Email) &&
                await _userRepository.IsEmailUniqueAsync(request.Email, user.Id))
            {
                user.Email = request.Email.Trim();
            }

            user.UpdateTimestamp();
            _userRepository.UpdateUser(user);

            var existingProfiles = (await _userRepository.GetProfilesByUserAsync(user.Id)).ToList();
            var patientProfileEntity = existingProfiles.FirstOrDefault(p => p.ProfileType == ProfileType.Patient);
            if (patientProfileEntity is null)
            {
                var created = await CreatePatientProfile(user.Id, new CreatePatientProfileRequest
                {
                    SNILS = request.SNILS,
                    InsuranceNumber = request.InsuranceNumber,
                    ResidenceAddress = request.ResidenceAddress,
                    RegistrationAddress = request.RegistrationAddress
                });
                created.IsActive = existingProfiles.All(p => !p.IsActive);
                await _userRepository.AddProfileAsync(created);
                await AssignDefaultRoleAsync(user.Id, created);
            }
            else if (patientProfileEntity.PatientProfile is not null)
            {
                var patient = patientProfileEntity.PatientProfile;
                if (!string.IsNullOrWhiteSpace(request.SNILS))
                    patient.SNILS = _encryptionService.Encrypt(request.SNILS);
                if (!string.IsNullOrWhiteSpace(request.InsuranceNumber))
                    patient.InsuranceNumber = _encryptionService.Encrypt(request.InsuranceNumber);
                if (request.ResidenceAddress is not null)
                    patient.ResidenceAddress = MapAddress(request.ResidenceAddress);
                if (request.RegistrationAddress is not null)
                    patient.RegistrationAddress = MapAddress(request.RegistrationAddress);
                _userRepository.UpdateProfile(patientProfileEntity);
            }

            await _userRepository.SaveChangesAsync();
            return await GetUserWithProfilesAsync(userPublicId);
        }

        private static string NormalizeSex(string value) => value.Trim().ToUpperInvariant() switch
        {
            "M" or "MALE" or "МУЖ" or "МУЖСКОЙ" => nameof(Sex.Male),
            "F" or "FEMALE" or "ЖЕН" or "ЖЕНСКИЙ" => nameof(Sex.Female),
            _ => value
        };

        private static Address? MapAddress(AddressDto? dto)
        {
            if (dto is null)
                return null;
            if (string.IsNullOrWhiteSpace(dto.PostCode) &&
                string.IsNullOrWhiteSpace(dto.Country) &&
                string.IsNullOrWhiteSpace(dto.Region) &&
                string.IsNullOrWhiteSpace(dto.City) &&
                string.IsNullOrWhiteSpace(dto.Street) &&
                string.IsNullOrWhiteSpace(dto.House) &&
                string.IsNullOrWhiteSpace(dto.Flat) &&
                string.IsNullOrWhiteSpace(dto.Area))
                return null;

            return new Address
            {
                PostCode = dto.PostCode,
                Country = dto.Country,
                Region = dto.Region,
                City = dto.City,
                Area = dto.Area,
                Street = dto.Street,
                House = dto.House,
                Flat = dto.Flat
            };
        }

        private static AddressDto? MapAddressDto(Address? address)
        {
            if (address is null)
                return null;
            return new AddressDto
            {
                PostCode = address.PostCode,
                Country = address.Country,
                Region = address.Region,
                City = address.City,
                Area = address.Area,
                Street = address.Street,
                House = address.House,
                Flat = address.Flat
            };
        }

        public async Task<UserWithProfilesDto?> GetUserByPublicIdOrProfileIdAsync(Guid id)
        {
            try
            {
                return await GetUserWithProfilesAsync(id);
            }
            catch (UserNotFoundException)
            {
                /* maybe ProfileId */
            }

            var roles = await _userRoleRepository.GetByProfileAsync(id);
            var userId = roles.Select(r => r.UserId).FirstOrDefault();
            if (userId == Guid.Empty)
                return null;

            var user = await _userRepository.GetByIdAsync(userId);
            if (user is null)
                return null;

            return await GetUserWithProfilesAsync(user.PublicId);
        }

        public async Task<IReadOnlyList<Guid>> ResolveIdentityIdsAsync(Guid id)
        {
            var ids = new HashSet<Guid> { id };
            var user = await GetUserByPublicIdOrProfileIdAsync(id);
            if (user is null)
                return ids.ToList();

            if (user.PublicId != Guid.Empty)
                ids.Add(user.PublicId);
            foreach (var profile in user.Profiles)
            {
                if (profile.ProfileId != Guid.Empty)
                    ids.Add(profile.ProfileId);
            }

            return ids.ToList();
        }

        private string? DecryptIfNeeded(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;
            try
            {
                return _encryptionService.Decrypt(value);
            }
            catch (FormatException)
            {
                return value;
            }
            catch (System.Security.Cryptography.CryptographicException)
            {
                return value;
            }
        }
    }
    public class PatientProfileData
    {
        public string? InsuranceNumber { get; set; }
        public string? SNILS { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
        public AddressDto? ResidenceAddress { get; set; }
        public AddressDto? RegistrationAddress { get; set; }
        public List<Guid> DoctorIds { get; set; } = new();
        public List<Guid> OrganizationIds { get; set; } = new();
    }

    public class DoctorProfileData
    {
        public string Specialization { get; set; } = string.Empty;
        public string DiplomaNumber { get; set; } = string.Empty;
        public string? DiplomaSeries { get; set; }
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime CertificateExpiryDate { get; set; }
        public string Category { get; set; } = string.Empty;
        public string? AcademicDegree { get; set; }
        public string? Biography { get; set; }
        public double Rating { get; set; }
        public bool IsCertificateValid { get; set; }
    }

    public class OrganizationProfileData
    {
        public string LegalName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string INN { get; set; } = string.Empty;
        public string? KPP { get; set; }
        public string OGRN { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
    }
}
