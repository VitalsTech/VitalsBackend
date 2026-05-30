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
    }

    public class MultiProfileUserService : IMultiProfileUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IEncryptionService _encryptionService;

        public MultiProfileUserService(
            IUserRepository userRepository,
            IMapper mapper,
            IEncryptionService encryptionService)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _encryptionService = encryptionService;
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

            var existingProfile = await _userRepository.GetProfileByUserAndTypeAsync(user.Id, profileType);
            if (existingProfile != null)
                throw new InvalidOperationException($"User already has a {request.ProfileType} profile");

            Profile profile = null!;

            profile = profileType switch
            {
                ProfileType.Patient when request.PatientProfile != null => await CreatePatientProfile(user.Id, request.PatientProfile),
                ProfileType.Doctor when request.DoctorProfile != null => await CreateDoctorProfile(user.Id, request.DoctorProfile),
                ProfileType.Organization when request.OrganizationProfile != null => await CreateOrganizationProfile(user.Id, request.OrganizationProfile),
                _ => throw new ValidationException($"{profileType} profile data must be provided")
            };

            await _userRepository.AddProfileAsync(profile);
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
                Allergies = request.Allergies
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
                    InsuranceNumber = profile.PatientProfile.InsuranceNumber,
                    SNILS = profile.PatientProfile.SNILS,
                    BloodType = profile.PatientProfile.BloodType?.ToString(),
                    Allergies = profile.PatientProfile.Allergies,
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
    }
    public class PatientProfileData
    {
        public string? InsuranceNumber { get; set; }
        public string? SNILS { get; set; }
        public string? BloodType { get; set; }
        public string? Allergies { get; set; }
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
