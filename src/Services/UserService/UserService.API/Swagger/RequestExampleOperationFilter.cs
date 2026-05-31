using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using UserService.Application.DTOs.Common;

namespace UserService.API.Swagger
{
    public class RequestExampleOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.RequestBody?.Content == null)
                return;

            var bodyType = context.ApiDescription.ParameterDescriptions
                .FirstOrDefault(parameter => parameter.Source == BindingSource.Body)
                ?.Type;

            if (bodyType == null)
                return;

            var example = CreateExample(bodyType);
            if (example == null)
                return;

            foreach (var content in operation.RequestBody.Content.Values)
                content.Example = example;
        }

        private static IOpenApiAny? CreateExample(Type bodyType)
        {
            if (bodyType == typeof(CreateUserWithProfileRequest))
            {
                return Obj(
                    ("phoneNumber", Str("89000936941")),
                    ("email", Str("user@example.com")),
                    ("firstName", Str("Иван")),
                    ("secondName", Str("Иванович")),
                    ("surename", Str("Петров")),
                    ("birthDate", Str("1990-01-01T00:00:00Z")),
                    ("sex", Str("Male")),
                    ("patientProfile", new OpenApiNull()),
                    ("doctorProfile", Obj(
                        ("specialization", Str("Терапевт")),
                        ("diplomaNumber", Str("123456")),
                        ("diplomaSeries", Str("AB")),
                        ("certificateNumber", Str("CERT-123")),
                        ("certificateExpiryDate", Str("2030-01-01T00:00:00Z")),
                        ("organizationId", new OpenApiNull()),
                        ("category", Str("None")),
                        ("academicDegree", Str("к.м.н.")),
                        ("biography", Str("Опыт работы 10 лет.")))),
                    ("organizationProfile", new OpenApiNull()));
            }

            if (bodyType == typeof(AddProfileToExistingUserRequest))
            {
                return Obj(
                    ("userPublicId", Str("87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea")),
                    ("profileType", Str("Doctor")),
                    ("patientProfile", new OpenApiNull()),
                    ("doctorProfile", Obj(
                        ("specialization", Str("Терапевт")),
                        ("diplomaNumber", Str("123456")),
                        ("diplomaSeries", Str("AB")),
                        ("certificateNumber", Str("CERT-123")),
                        ("certificateExpiryDate", Str("2030-01-01T00:00:00Z")),
                        ("organizationId", new OpenApiNull()),
                        ("category", Str("None")),
                        ("academicDegree", Str("к.м.н.")),
                        ("biography", Str("Опыт работы 10 лет.")))),
                    ("organizationProfile", new OpenApiNull()));
            }

            if (bodyType == typeof(SwitchActiveProfileRequest))
            {
                return Obj(
                    ("userPublicId", Str("87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea")),
                    ("profileId", Str("ec5ee1d5-677d-47ff-b2f0-af361cf8692a")));
            }

            if (bodyType == typeof(UserSearchRequest))
            {
                return Obj(
                    ("phoneNumber", Str("89000936941")),
                    ("email", new OpenApiNull()),
                    ("firstName", new OpenApiNull()),
                    ("surename", new OpenApiNull()),
                    ("userType", new OpenApiNull()),
                    ("specialization", new OpenApiNull()),
                    ("organizationRole", new OpenApiNull()),
                    ("page", Int(0)),
                    ("pageSize", Int(20)));
            }

            if (bodyType == typeof(UserStatusRequest))
            {
                return Obj(
                    ("isActive", new OpenApiBoolean(true)),
                    ("reason", Str("Ручное изменение администратором")));
            }

            if (bodyType == typeof(UserBlockRequest))
            {
                return Obj(
                    ("blockedUntil", Str("2030-01-01T00:00:00Z")),
                    ("blockReason", Str("Нарушение правил сервиса")));
            }

            if (bodyType == typeof(PermissionCheckRequest))
            {
                return Obj(
                    ("userPublicId", Str("87cf5fbd-5c35-4bac-9ec2-53a4fed3f9ea")),
                    ("permission", Str("users.block")));
            }

            return null;
        }

        private static OpenApiObject Obj(params (string Name, IOpenApiAny Value)[] properties)
        {
            var obj = new OpenApiObject();
            foreach (var (name, value) in properties)
                obj[name] = value;
            return obj;
        }

        private static OpenApiString Str(string value) => new(value);

        private static OpenApiInteger Int(int value) => new(value);
    }
}
