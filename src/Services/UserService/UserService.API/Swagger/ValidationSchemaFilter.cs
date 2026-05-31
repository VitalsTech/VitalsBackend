using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using UserService.Application.DTOs.Common;
using UserService.Application.DTOs.Doctor;
using UserService.Application.DTOs.Organization;
using UserService.Application.DTOs.Patient;
using UserService.Domain.Enums;

namespace UserService.API.Swagger
{
    public class ValidationSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                ConfigureEnum(schema, context.Type);
                return;
            }

            if (schema.Properties == null)
                return;

            if (context.Type == typeof(CreateUserWithProfileRequest))
            {
                schema.Description = "Создание пользователя. Нужно передать ровно один профиль: patientProfile, doctorProfile или organizationProfile.";
                EnumString(schema, "sex", "Допустимые значения: Male, Female.", new[] { "Male", "Female" }, "Male");
                Describe(schema, "patientProfile", "Передается только для профиля пациента. Ровно один из patientProfile/doctorProfile/organizationProfile.");
                Describe(schema, "doctorProfile", "Передается только для профиля врача. Ровно один из patientProfile/doctorProfile/organizationProfile.");
                Describe(schema, "organizationProfile", "Передается только для профиля организации. Ровно один из patientProfile/doctorProfile/organizationProfile.");
            }
            else if (context.Type == typeof(AddProfileToExistingUserRequest))
            {
                schema.Description = "Добавление профиля существующему пользователю. profileType должен соответствовать переданному объекту профиля.";
                EnumString(schema, "profileType", "Допустимые значения: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
                Describe(schema, "patientProfile", "Передается только при profileType = Patient.");
                Describe(schema, "doctorProfile", "Передается только при profileType = Doctor.");
                Describe(schema, "organizationProfile", "Передается только при profileType = Organization.");
            }
            else if (context.Type == typeof(CreatePatientProfileRequest))
            {
                schema.Description = "Данные профиля пациента.";
                EnumString(schema, "bloodType", "Допустимые значения: APositive, ANegative, BPositive, BNegative, ABPositive, ABNegative, OPositive, ONegative.",
                    Enum.GetNames<BloodType>(), "APositive");
            }
            else if (context.Type == typeof(CreateDoctorProfileRequest))
            {
                schema.Description = "Данные профиля врача.";
                EnumString(schema, "category", "Допустимые значения: None, Second, First, Highest.", Enum.GetNames<DoctorCategory>(), "None");
            }
            else if (context.Type == typeof(CreateOrganizationProfileRequest))
            {
                schema.Description = "Данные профиля организации.";
                EnumString(schema, "role", "Допустимые значения: Clinic, Laboratory, Pharmacy.", Enum.GetNames<OrganizationRole>(), "Clinic");
            }
            else if (context.Type == typeof(UserSearchRequest))
            {
                schema.Description = "Поиск пользователей. Пагинация zero-based: первая страница page = 0.";
                EnumString(schema, "userType", "Необязательный фильтр. Возможные значения: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
                EnumString(schema, "organizationRole", "Необязательный фильтр. Возможные значения: Clinic, Laboratory, Pharmacy.", Enum.GetNames<OrganizationRole>(), "Clinic");
            }
            else if (context.Type == typeof(UserBlockRequest))
            {
                schema.Description = "Блокировка пользователя. Если blockedUntil не указан, пользователь становится неактивным бессрочно до unblock.";
            }
            else if (context.Type == typeof(ActiveProfileResponse))
            {
                EnumString(schema, "profileType", "Тип активного профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
                Describe(schema, "profileData", "Данные активного профиля. Структура зависит от profileType.");
            }
            else if (context.Type == typeof(ProfileInfoDto))
            {
                EnumString(schema, "profileType", "Тип профиля: Patient, Doctor, Organization.", new[] { "Patient", "Doctor", "Organization" }, "Doctor");
                Describe(schema, "data", "Данные профиля. Структура зависит от profileType.");
            }
        }

        private static void ConfigureEnum(OpenApiSchema schema, Type type)
        {
            var names = Enum.GetNames(type);
            schema.Type = "string";
            schema.Format = null;
            schema.Description = $"Допустимые значения: {string.Join(", ", names)}.";
            schema.Enum = names.Select(value => (IOpenApiAny)new OpenApiString(value)).ToList();
            schema.Example = new OpenApiString(names.First());
        }

        private static void EnumString(OpenApiSchema schema, string propertyName, string description, IEnumerable<string> values, string example)
        {
            if (!TryGetProperty(schema, propertyName, out var property))
                return;

            property.Type = "string";
            property.Description = MergeDescription(property.Description, description);
            property.Enum = values.Select(value => (IOpenApiAny)new OpenApiString(value)).ToList();
            property.Example = new OpenApiString(example);
        }

        private static void Describe(OpenApiSchema schema, string propertyName, string description)
        {
            if (TryGetProperty(schema, propertyName, out var property))
                property.Description = MergeDescription(property.Description, description);
        }

        private static bool TryGetProperty(OpenApiSchema schema, string propertyName, out OpenApiSchema property)
        {
            var actualName = schema.Properties.Keys.FirstOrDefault(key => string.Equals(key, propertyName, StringComparison.OrdinalIgnoreCase));
            if (actualName != null)
                return schema.Properties.TryGetValue(actualName, out property!);

            property = null!;
            return false;
        }

        private static string MergeDescription(string? current, string description)
        {
            if (string.IsNullOrWhiteSpace(current))
                return description;

            if (current.Contains(description, StringComparison.Ordinal))
                return current;

            return $"{current} {description}";
        }
    }
}
