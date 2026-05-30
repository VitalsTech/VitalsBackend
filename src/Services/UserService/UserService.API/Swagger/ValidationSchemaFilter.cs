using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using UserService.API.Controllers;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Doctor;
using UserService.Application.DTOs.Organization;
using UserService.Application.DTOs.Patient;
using UserService.Application.Interfaces;
using UserService.Domain.Entities;
using UserService.Domain.Enums;

namespace UserService.API.Swagger
{
    public class ValidationSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                ConfigureEnumSchema(schema, context.Type);
                return;
            }

            if (schema.Properties == null)
                return;

            if (context.Type == typeof(CreateUserWithProfileRequest))
                ConfigureCreateUserWithProfileRequest(schema);
            else if (context.Type == typeof(AddProfileToExistingUserRequest))
                ConfigureAddProfileToExistingUserRequest(schema);
            else if (context.Type == typeof(CreatePatientProfileRequest))
                ConfigureCreatePatientProfileRequest(schema);
            else if (context.Type == typeof(CreateDoctorProfileRequest))
                ConfigureCreateDoctorProfileRequest(schema);
            else if (context.Type == typeof(CreateOrganizationProfileRequest))
                ConfigureCreateOrganizationProfileRequest(schema);
            else if (context.Type == typeof(AddressDto) || context.Type == typeof(Address))
                ConfigureAddress(schema);
            else if (context.Type == typeof(SwitchActiveProfileRequest))
                ConfigureSwitchActiveProfileRequest(schema);
            else if (context.Type == typeof(UserSearchRequest))
                ConfigureUserSearchRequest(schema);
            else if (context.Type == typeof(UserStatusRequest))
                ConfigureUserStatusRequest(schema);
            else if (context.Type == typeof(UserBlockRequest))
                ConfigureUserBlockRequest(schema);
            else if (context.Type == typeof(PermissionCheckRequest))
                ConfigurePermissionCheckRequest(schema);
            else if (context.Type == typeof(ActiveProfileResponse))
                ConfigureActiveProfileResponse(schema);
            else if (context.Type == typeof(UserWithProfilesDto))
                ConfigureUserWithProfilesDto(schema);
            else if (context.Type == typeof(ProfileInfoDto))
                ConfigureProfileInfoDto(schema);
            else if (context.Type == typeof(UserDto))
                ConfigureUserDto(schema);
            else if (context.Type == typeof(CreateUserResponse))
                ConfigureCreateUserResponse(schema);
            else if (context.Type == typeof(RoleDto))
                ConfigureRoleDto(schema);
            else if (context.Type == typeof(PermissionCheckResponse))
                ConfigurePermissionCheckResponse(schema);
            else if (context.Type == typeof(UserRoleResponse))
                ConfigureUserRoleResponse(schema);
            else if (context.Type == typeof(SearchResponse<UserDto>))
                ConfigureSearchResponse(schema, "Ответ поиска пользователей.");
            else if (context.Type == typeof(SearchResult<UserDto>))
                ConfigureSearchResponse(schema, "Ответ поиска пользователей.");
            else if (context.Type == typeof(AssignRoleRequest))
                ConfigureAssignRoleRequest(schema);
            else if (context.Type == typeof(UnassignRoleRequest))
                ConfigureAssignRoleRequest(schema);
            else if (context.Type == typeof(UserRolesResponse))
                ConfigureUserRolesResponse(schema);
            else if (context.Type == typeof(ProfileRolesResponse))
                ConfigureProfileRolesResponse(schema);
            else if (context.Type == typeof(Role))
                ConfigureRole(schema);
            else if (context.Type == typeof(Permission))
                ConfigurePermission(schema);
            else if (context.Type == typeof(UserRole))
                ConfigureUserRole(schema);
            else if (context.Type == typeof(Profile))
                ConfigureProfile(schema);
            else if (context.Type == typeof(PatientProfile))
                ConfigurePatientProfile(schema);
            else if (context.Type == typeof(DoctorProfile))
                ConfigureDoctorProfile(schema);
            else if (context.Type == typeof(OrganizationProfile))
                ConfigureOrganizationProfile(schema);
        }

        private static void ConfigureCreateUserWithProfileRequest(OpenApiSchema schema)
        {
            schema.Description = "Создание пользователя. Нужно передать ровно один профиль: patientProfile, doctorProfile или organizationProfile.";

            Required(schema, "phoneNumber", "firstName", "surename", "birthDate", "sex");
            String(schema, "phoneNumber", "Обязательное поле. Телефон: 10-15 цифр.", pattern: "^[0-9]{10,15}$", example: "89000936941");
            String(schema, "email", "Необязательное поле. Должно быть валидным email.", format: "email", example: "user@example.com");
            String(schema, "firstName", "Обязательное поле. Имя, максимум 100 символов.", maxLength: 100, example: "Иван");
            String(schema, "secondName", "Необязательное поле. Отчество.", maxLength: 100, example: "Иванович");
            String(schema, "surename", "Обязательное поле. Фамилия, максимум 100 символов.", maxLength: 100, example: "Петров");
            Date(schema, "birthDate", "Обязательное поле. Дата рождения: не в будущем и не старше 120 лет.", "1990-01-01T00:00:00Z");
            EnumString(schema, "sex", "Обязательное поле. Допустимые значения: Male, Female.", new[] { "Male", "Female" }, "Male");
            Describe(schema, "patientProfile", "Передается только если создается профиль пациента. Ровно один из patientProfile/doctorProfile/organizationProfile.");
            Describe(schema, "doctorProfile", "Передается только если создается профиль врача. Ровно один из patientProfile/doctorProfile/organizationProfile.");
            Describe(schema, "organizationProfile", "Передается только если создается профиль организации. Ровно один из patientProfile/doctorProfile/organizationProfile.");
        }

        private static void ConfigureAddProfileToExistingUserRequest(OpenApiSchema schema)
        {
            schema.Description = "Добавление профиля существующему пользователю. profileType должен соответствовать переданному объекту профиля.";

            Required(schema, "userPublicId", "profileType");
            Guid(schema, "userPublicId", "Обязательное поле. PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            EnumString(schema, "profileType", "Обязательное поле. Допустимые значения: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            Describe(schema, "patientProfile", "Передается только при profileType = Patient.");
            Describe(schema, "doctorProfile", "Передается только при profileType = Doctor.");
            Describe(schema, "organizationProfile", "Передается только при profileType = Organization.");
        }

        private static void ConfigureCreatePatientProfileRequest(OpenApiSchema schema)
        {
            schema.Description = "Данные профиля пациента.";

            String(schema, "insuranceNumber", "Необязательное поле. Номер полиса, максимум 20 символов.", maxLength: 20, example: "1234567890123456");
            String(schema, "snils", "Необязательное поле. СНИЛС: ровно 11 цифр.", pattern: "^\\d{11}$", example: "12345678901");
            EnumString(schema, "bloodType", "Необязательное поле. Допустимые значения: APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative.",
                Enum.GetNames<BloodType>(), "APositive");
            String(schema, "allergies", "Необязательное поле. Аллергии или важные медицинские пометки.", example: "Пенициллин");
        }

        private static void ConfigureCreateDoctorProfileRequest(OpenApiSchema schema)
        {
            schema.Description = "Данные профиля врача.";

            Required(schema, "specialization", "diplomaNumber", "certificateNumber", "certificateExpiryDate", "category");
            String(schema, "specialization", "Обязательное поле. Специализация, максимум 100 символов.", maxLength: 100, example: "Терапевт");
            String(schema, "diplomaNumber", "Обязательное поле. Номер диплома, максимум 50 символов.", maxLength: 50, example: "123456");
            String(schema, "diplomaSeries", "Необязательное поле. Серия диплома.", maxLength: 50, example: "AB");
            String(schema, "certificateNumber", "Обязательное поле. Номер сертификата, максимум 50 символов.", maxLength: 50, example: "CERT-123");
            Date(schema, "certificateExpiryDate", "Обязательное поле. Дата окончания сертификата должна быть в будущем.", "2030-01-01T00:00:00Z");
            Guid(schema, "organizationId", "Необязательное поле. Id организации врача.", "11111111-1111-1111-1111-111111111111");
            EnumString(schema, "category", "Обязательное поле. Допустимые значения: None, Second, First, Highest.", Enum.GetNames<DoctorCategory>(), "None");
            String(schema, "academicDegree", "Необязательное поле. Ученая степень, максимум 200 символов.", maxLength: 200, example: "к.м.н.");
            String(schema, "biography", "Необязательное поле. Биография, максимум 2000 символов.", maxLength: 2000, example: "Опыт работы 10 лет.");
        }

        private static void ConfigureCreateOrganizationProfileRequest(OpenApiSchema schema)
        {
            schema.Description = "Данные профиля организации.";

            Required(schema, "legalName", "displayName", "inn", "ogrn", "role");
            String(schema, "legalName", "Обязательное поле. Юридическое название, максимум 200 символов.", maxLength: 200, example: "ООО Клиника");
            String(schema, "displayName", "Обязательное поле. Отображаемое название, максимум 100 символов.", maxLength: 100, example: "Клиника");
            String(schema, "inn", "Обязательное поле. ИНН: 10 или 12 цифр.", pattern: "^\\d{10}$|^\\d{12}$", example: "7701234567");
            String(schema, "kpp", "Необязательное поле. КПП.", example: "770101001");
            String(schema, "ogrn", "Обязательное поле. ОГРН: 13 или 15 цифр.", pattern: "^\\d{13}$|^\\d{15}$", example: "1027700132195");
            Describe(schema, "legalAddress", "Необязательное поле. Юридический адрес.");
            String(schema, "contactPhone", "Необязательное поле. Контактный телефон: 10-15 цифр.", pattern: "^[0-9]{10,15}$", example: "89000936941");
            String(schema, "contactEmail", "Необязательное поле. Контактный email.", format: "email", example: "clinic@example.com");
            EnumString(schema, "role", "Обязательное поле. Допустимые значения: Clinic, Laboratory, Pharmacy.", Enum.GetNames<OrganizationRole>(), "Clinic");
            Guid(schema, "administratorId", "Необязательное поле. Id администратора организации.", "22222222-2222-2222-2222-222222222222");
        }

        private static void ConfigureAddress(OpenApiSchema schema)
        {
            schema.Description = "Адрес.";

            String(schema, "postCode", "Почтовый индекс.", example: "620000");
            String(schema, "country", "Страна.", example: "Россия");
            String(schema, "region", "Регион.", example: "Свердловская область");
            String(schema, "city", "Город.", example: "Екатеринбург");
            String(schema, "area", "Район.", example: "Ленинский");
            String(schema, "street", "Улица.", example: "Ленина");
            String(schema, "house", "Дом.", example: "1");
            String(schema, "flat", "Квартира/офис.", example: "10");
        }

        private static void ConfigureSwitchActiveProfileRequest(OpenApiSchema schema)
        {
            Required(schema, "userPublicId", "profileId");
            Guid(schema, "userPublicId", "Обязательное поле. PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            Guid(schema, "profileId", "Обязательное поле. Id профиля, который нужно сделать активным.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
        }

        private static void ConfigureUserSearchRequest(OpenApiSchema schema)
        {
            schema.Description = "Поиск пользователей. Пагинация zero-based: первая страница page = 0.";

            Integer(schema, "page", "Номер страницы, начиная с 0.", minimum: 0, example: 0);
            Integer(schema, "pageSize", "Размер страницы: от 1 до 100.", minimum: 1, maximum: 100, example: 20);
            String(schema, "phoneNumber", "Фильтр по телефону, частичное совпадение.", example: "89000936941");
            String(schema, "email", "Фильтр по email, частичное совпадение.", example: "user@example.com");
            String(schema, "firstName", "Фильтр по имени, частичное совпадение.", example: "Иван");
            String(schema, "surename", "Фильтр по фамилии, частичное совпадение.", example: "Петров");
            EnumString(schema, "userType", "Необязательный фильтр. Возможные значения зависят от профилей: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            String(schema, "specialization", "Необязательный фильтр по специализации врача.", example: "Терапевт");
            EnumString(schema, "organizationRole", "Необязательный фильтр по роли организации: Clinic, Laboratory, Pharmacy.", Enum.GetNames<OrganizationRole>(), "Clinic");
        }

        private static void ConfigureUserStatusRequest(OpenApiSchema schema)
        {
            Required(schema, "isActive");
            Boolean(schema, "isActive", "Обязательное поле. true - активировать пользователя, false - деактивировать.", true);
            String(schema, "reason", "Необязательное поле. Причина изменения статуса.", example: "Ручное изменение администратором");
        }

        private static void ConfigureUserBlockRequest(OpenApiSchema schema)
        {
            Date(schema, "blockedUntil", "Необязательное поле. Если указано, дата должна быть в будущем. Если null - блокировка бессрочная через IsActive = false.", "2030-01-01T00:00:00Z");
            String(schema, "blockReason", "Необязательное поле. Причина блокировки.", example: "Нарушение правил сервиса");
        }

        private static void ConfigurePermissionCheckRequest(OpenApiSchema schema)
        {
            Required(schema, "userPublicId", "permission");
            Guid(schema, "userPublicId", "Обязательное поле. PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            String(schema, "permission", "Обязательное поле. Код права доступа.", example: "users.block");
        }

        private static void ConfigureActiveProfileResponse(OpenApiSchema schema)
        {
            schema.Description = "Ответ переключения активного профиля.";
            Required(schema, "profileId", "profileType", "profileData");
            Guid(schema, "profileId", "Id активного профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            EnumString(schema, "profileType", "Тип активного профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            Describe(schema, "profileData", "Данные активного профиля. Структура зависит от profileType.");
        }

        private static void ConfigureUserWithProfilesDto(OpenApiSchema schema)
        {
            schema.Description = "Пользователь со списком профилей.";
            Required(schema, "publicId", "phoneNumber", "firstName", "surename", "birthDate", "sex", "isActive", "createdAt", "updatedAt", "profiles");
            Guid(schema, "publicId", "PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            String(schema, "phoneNumber", "Телефон: 10-15 цифр.", pattern: "^[0-9]{10,15}$", example: "89000936941");
            String(schema, "email", "Email пользователя.", format: "email", example: "user@example.com");
            String(schema, "firstName", "Имя, максимум 100 символов.", maxLength: 100, example: "Иван");
            String(schema, "secondName", "Отчество.", maxLength: 100, example: "Иванович");
            String(schema, "surename", "Фамилия, максимум 100 символов.", maxLength: 100, example: "Петров");
            Date(schema, "birthDate", "Дата рождения.", "1990-01-01T00:00:00Z");
            EnumString(schema, "sex", "Пол: Male, Female.", new[] { "Male", "Female" }, "Male");
            Boolean(schema, "isActive", "Активен ли пользователь.", true);
            Date(schema, "createdAt", "Дата создания пользователя.", "2026-01-01T00:00:00Z");
            Date(schema, "updatedAt", "Дата последнего изменения пользователя.", "2026-01-01T00:00:00Z");
            Describe(schema, "profiles", "Список профилей пользователя.");
            Guid(schema, "activeProfileId", "Id текущего активного профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
        }

        private static void ConfigureProfileInfoDto(OpenApiSchema schema)
        {
            schema.Description = "Краткая информация о профиле пользователя.";
            Required(schema, "profileId", "profileType", "isActive");
            Guid(schema, "profileId", "Id профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            EnumString(schema, "profileType", "Тип профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            Boolean(schema, "isActive", "Активен ли профиль.", true);
            Describe(schema, "data", "Данные профиля. Структура зависит от profileType.");
        }

        private static void ConfigureUserDto(OpenApiSchema schema)
        {
            schema.Description = "Краткая модель пользователя для административных списков.";
            Required(schema, "publicId", "phoneNumber", "firstName", "surename", "birthDate", "sex", "createdAt", "updatedAt", "isActive");
            Guid(schema, "publicId", "PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            String(schema, "phoneNumber", "Телефон: 10-15 цифр.", pattern: "^[0-9]{10,15}$", example: "89000936941");
            String(schema, "email", "Email пользователя.", format: "email", example: "user@example.com");
            String(schema, "firstName", "Имя, максимум 100 символов.", maxLength: 100, example: "Иван");
            String(schema, "secondName", "Отчество.", maxLength: 100, example: "Иванович");
            String(schema, "surename", "Фамилия, максимум 100 символов.", maxLength: 100, example: "Петров");
            Date(schema, "birthDate", "Дата рождения.", "1990-01-01T00:00:00Z");
            EnumString(schema, "sex", "Пол: Male, Female.", new[] { "Male", "Female" }, "Male");
            String(schema, "insuranceNumber", "Номер полиса пациента, если есть.", maxLength: 20, example: "1234567890123456");
            String(schema, "snils", "СНИЛС пациента: 11 цифр, если есть.", pattern: "^\\d{11}$", example: "12345678901");
            Describe(schema, "registrationAddress", "Адрес регистрации, если есть.");
            Describe(schema, "actualAddress", "Фактический адрес, если есть.");
            Date(schema, "createdAt", "Дата создания пользователя.", "2026-01-01T00:00:00Z");
            Date(schema, "updatedAt", "Дата последнего изменения пользователя.", "2026-01-01T00:00:00Z");
            EnumString(schema, "userType", "Тип пользователя/профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            Boolean(schema, "isActive", "Активен ли пользователь.", true);
            Date(schema, "blockedUntil", "Дата окончания блокировки, если пользователь заблокирован на срок.", "2030-01-01T00:00:00Z");
            String(schema, "blockReason", "Причина блокировки.", example: "Нарушение правил сервиса");
        }

        private static void ConfigureCreateUserResponse(OpenApiSchema schema)
        {
            schema.Description = "Ответ создания пользователя.";
            Required(schema, "publicId", "message");
            Guid(schema, "publicId", "PublicId созданного пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            String(schema, "message", "Сообщение о результате операции.", example: "User created successfully");
        }

        private static void ConfigureRoleDto(OpenApiSchema schema)
        {
            schema.Description = "Роль с набором прав.";
            Required(schema, "name", "permissions");
            String(schema, "name", "Название роли.", example: "Admin");
            String(schema, "description", "Описание роли.", example: "Администратор системы");
            Describe(schema, "permissions", "Список кодов прав роли, например users.block.");
        }

        private static void ConfigurePermissionCheckResponse(OpenApiSchema schema)
        {
            schema.Description = "Результат проверки права пользователя.";
            Required(schema, "hasPermission");
            Boolean(schema, "hasPermission", "Есть ли право.", false);
            String(schema, "reason", "Причина отказа, если права нет.", example: "User is blocked or inactive");
        }

        private static void ConfigureUserRoleResponse(OpenApiSchema schema)
        {
            schema.Description = "Роли и права пользователя.";
            Required(schema, "userPublicId", "roles", "permissions");
            Guid(schema, "userPublicId", "PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            Describe(schema, "roles", "Названия ролей пользователя.");
            Describe(schema, "permissions", "Коды прав пользователя.");
        }

        private static void ConfigureSearchResponse(OpenApiSchema schema, string description)
        {
            schema.Description = description;
            Required(schema, "items", "totalCount", "page", "pageSize", "totalPages");
            Describe(schema, "items", "Элементы текущей страницы.");
            Integer(schema, "totalCount", "Общее количество найденных элементов.", minimum: 0, example: 1);
            Integer(schema, "page", "Номер страницы, начиная с 0.", minimum: 0, example: 0);
            Integer(schema, "pageSize", "Размер страницы.", minimum: 1, maximum: 100, example: 20);
            Integer(schema, "totalPages", "Общее количество страниц.", minimum: 0, example: 1);
        }

        private static void ConfigureAssignRoleRequest(OpenApiSchema schema)
        {
            schema.Description = "Назначение или снятие роли с профиля пользователя.";
            Required(schema, "roleName", "profileId");
            String(schema, "roleName", "Обязательное поле. Название роли.", example: "Admin");
            Guid(schema, "profileId", "Обязательное поле. Id профиля пользователя.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
        }

        private static void ConfigureUserRolesResponse(OpenApiSchema schema)
        {
            schema.Description = "Роли пользователя по профилям.";
            Required(schema, "userPublicId", "profiles");
            Guid(schema, "userPublicId", "PublicId пользователя.", "87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea");
            Describe(schema, "profiles", "Профили пользователя с ролями и правами.");
        }

        private static void ConfigureProfileRolesResponse(OpenApiSchema schema)
        {
            schema.Description = "Роли и права конкретного профиля.";
            Required(schema, "profileId", "profileType", "roles", "permissions");
            Guid(schema, "profileId", "Id профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            EnumString(schema, "profileType", "Тип профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
            Describe(schema, "roles", "Названия ролей профиля.");
            Describe(schema, "permissions", "Коды прав профиля.");
        }

        private static void ConfigureRole(OpenApiSchema schema)
        {
            schema.Description = "Доменная роль.";
            Required(schema, "id", "name", "resource", "action", "createdAt");
            Guid(schema, "id", "Id роли.", "10000000-0000-0000-0000-000000000001");
            String(schema, "name", "Название роли.", example: "Admin");
            String(schema, "description", "Описание роли.", example: "Администратор системы");
        }

        private static void ConfigurePermission(OpenApiSchema schema)
        {
            schema.Description = "Право доступа.";
            Required(schema, "id", "name", "resource", "action", "createdAt");
            Guid(schema, "id", "Id права.", "10000000-0000-0000-0000-000000000017");
            String(schema, "name", "Код права.", example: "users.block");
            String(schema, "resource", "Ресурс права.", example: "Users");
            String(schema, "action", "Действие права.", example: "Block");
            String(schema, "description", "Описание права.", example: "Блокировка пользователей");
            Date(schema, "createdAt", "Дата создания права.", "2026-01-01T00:00:00Z");
        }

        private static void ConfigureUserRole(OpenApiSchema schema)
        {
            schema.Description = "Связь пользователя, роли и профиля.";
            Required(schema, "id", "userId", "roleId", "profileId", "assignedAt");
            Guid(schema, "id", "Id назначения роли.", "33333333-3333-3333-3333-333333333333");
            Guid(schema, "userId", "Внутренний Id пользователя.", "44444444-4444-4444-4444-444444444444");
            Guid(schema, "roleId", "Id роли.", "10000000-0000-0000-0000-000000000001");
            Guid(schema, "profileId", "Id профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            Date(schema, "assignedAt", "Дата назначения роли.", "2026-01-01T00:00:00Z");
            Guid(schema, "assignedBy", "Id администратора, назначившего роль.", "55555555-5555-5555-5555-555555555555");
        }

        private static void ConfigureProfile(OpenApiSchema schema)
        {
            schema.Description = "Базовая доменная модель профиля пользователя.";
            Required(schema, "id", "userId", "profileType", "isActive", "createdAt");
            Guid(schema, "id", "Id профиля.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            Guid(schema, "userId", "Внутренний Id пользователя.", "44444444-4444-4444-4444-444444444444");
            EnumString(schema, "profileType", "Тип профиля: Patient, Doctor, Organization.", Enum.GetNames<ProfileType>(), "Doctor");
            Boolean(schema, "isActive", "Активен ли профиль.", true);
            Date(schema, "createdAt", "Дата создания профиля.", "2026-01-01T00:00:00Z");
            Describe(schema, "patientProfile", "Данные пациента, если profileType = Patient.");
            Describe(schema, "doctorProfile", "Данные врача, если profileType = Doctor.");
            Describe(schema, "organizationProfile", "Данные организации, если profileType = Organization.");
        }

        private static void ConfigurePatientProfile(OpenApiSchema schema)
        {
            schema.Description = "Доменные данные профиля пациента.";
            Required(schema, "id");
            Guid(schema, "id", "Id профиля пациента.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            String(schema, "insuranceNumber", "Зашифрованный номер полиса.", maxLength: 20, example: "encrypted");
            String(schema, "snils", "Зашифрованный СНИЛС.", example: "encrypted");
            EnumString(schema, "bloodType", "Группа крови.", Enum.GetNames<BloodType>(), "APositive");
            String(schema, "allergies", "Аллергии.", example: "Пенициллин");
        }

        private static void ConfigureDoctorProfile(OpenApiSchema schema)
        {
            schema.Description = "Доменные данные профиля врача.";
            Required(schema, "id", "specialization", "diplomaNumber", "certificateNumber", "certificateExpiryDate", "category");
            Guid(schema, "id", "Id профиля врача.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            String(schema, "specialization", "Специализация, максимум 100 символов.", maxLength: 100, example: "Терапевт");
            String(schema, "diplomaNumber", "Номер диплома, максимум 50 символов.", maxLength: 50, example: "123456");
            String(schema, "diplomaSeries", "Серия диплома.", maxLength: 50, example: "AB");
            String(schema, "certificateNumber", "Номер сертификата, максимум 50 символов.", maxLength: 50, example: "CERT-123");
            Date(schema, "certificateExpiryDate", "Дата окончания сертификата.", "2030-01-01T00:00:00Z");
            Guid(schema, "organizationId", "Id организации врача.", "11111111-1111-1111-1111-111111111111");
            EnumString(schema, "category", "Категория врача: None, Second, First, Highest.", Enum.GetNames<DoctorCategory>(), "None");
            String(schema, "academicDegree", "Ученая степень, максимум 200 символов.", maxLength: 200, example: "к.м.н.");
            String(schema, "biography", "Биография, максимум 2000 символов.", maxLength: 2000, example: "Опыт работы 10 лет.");
            Describe(schema, "rating", "Рейтинг врача.");
            Describe(schema, "reviewCount", "Количество отзывов.");
        }

        private static void ConfigureOrganizationProfile(OpenApiSchema schema)
        {
            schema.Description = "Доменные данные профиля организации.";
            Required(schema, "id", "legalName", "displayName", "inn", "ogrn", "role");
            Guid(schema, "id", "Id профиля организации.", "ec5ee1d5-677d-47ff-b2f0-af361cf8692a");
            String(schema, "legalName", "Юридическое название, максимум 200 символов.", maxLength: 200, example: "ООО Клиника");
            String(schema, "displayName", "Отображаемое название, максимум 100 символов.", maxLength: 100, example: "Клиника");
            String(schema, "inn", "ИНН: 10 или 12 цифр.", pattern: "^\\d{10}$|^\\d{12}$", example: "7701234567");
            String(schema, "kpp", "КПП.", example: "770101001");
            String(schema, "ogrn", "ОГРН: 13 или 15 цифр.", pattern: "^\\d{13}$|^\\d{15}$", example: "1027700132195");
            Describe(schema, "legalAddress", "Юридический адрес.");
            String(schema, "contactPhone", "Контактный телефон: 10-15 цифр.", pattern: "^[0-9]{10,15}$", example: "89000936941");
            String(schema, "contactEmail", "Контактный email.", format: "email", example: "clinic@example.com");
            EnumString(schema, "role", "Роль организации: Clinic, Laboratory, Pharmacy.", Enum.GetNames<OrganizationRole>(), "Clinic");
            Guid(schema, "administratorId", "Id администратора организации.", "22222222-2222-2222-2222-222222222222");
        }

        private static void ConfigureEnumSchema(OpenApiSchema schema, Type type)
        {
            schema.Type = "string";
            schema.Format = null;
            schema.Description = $"Допустимые значения: {string.Join(", ", Enum.GetNames(type))}.";
            schema.Enum = Enum.GetNames(type).Select(value => (IOpenApiAny)new OpenApiString(value)).ToList();
            schema.Example = new OpenApiString(Enum.GetNames(type).First());
        }

        private static void Required(OpenApiSchema schema, params string[] names)
        {
            foreach (var name in names)
            {
                var propertyName = FindPropertyName(schema, name);
                if (propertyName != null)
                    schema.Required.Add(propertyName);
            }
        }

        private static void Describe(OpenApiSchema schema, string name, string description)
        {
            if (TryGetProperty(schema, name, out var property))
                property.Description = description;
        }

        private static void String(OpenApiSchema schema, string name, string description, string? pattern = null, string? format = null, int? maxLength = null, string? example = null)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Pattern = pattern;
            property.Format = format ?? property.Format;
            property.MaxLength = maxLength;
            if (example != null)
                property.Example = new OpenApiString(example);
        }

        private static void EnumString(OpenApiSchema schema, string name, string description, IEnumerable<string> values, string example)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Type = "string";
            property.Enum = values.Select(value => (IOpenApiAny)new OpenApiString(value)).ToList();
            property.Example = new OpenApiString(example);
        }

        private static void Guid(OpenApiSchema schema, string name, string description, string example)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Type = "string";
            property.Format = "uuid";
            property.Example = new OpenApiString(example);
        }

        private static void Date(OpenApiSchema schema, string name, string description, string example)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Type = "string";
            property.Format = "date-time";
            property.Example = new OpenApiString(example);
        }

        private static void Integer(OpenApiSchema schema, string name, string description, int minimum, int? maximum = null, int? example = null)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Minimum = minimum;
            property.Maximum = maximum;
            if (example.HasValue)
                property.Example = new OpenApiInteger(example.Value);
        }

        private static void Boolean(OpenApiSchema schema, string name, string description, bool example)
        {
            if (!TryGetProperty(schema, name, out var property))
                return;

            property.Description = description;
            property.Example = new OpenApiBoolean(example);
        }

        private static bool TryGetProperty(OpenApiSchema schema, string name, out OpenApiSchema property)
        {
            var propertyName = FindPropertyName(schema, name);
            if (propertyName != null)
                return schema.Properties.TryGetValue(propertyName, out property!);

            property = null!;
            return false;
        }

        private static string? FindPropertyName(OpenApiSchema schema, string name)
        {
            if (schema.Properties.ContainsKey(name))
                return name;

            var pascalName = char.ToUpperInvariant(name[0]) + name[1..];
            if (schema.Properties.ContainsKey(pascalName))
                return pascalName;

            return schema.Properties.Keys.FirstOrDefault(key => string.Equals(key, name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
