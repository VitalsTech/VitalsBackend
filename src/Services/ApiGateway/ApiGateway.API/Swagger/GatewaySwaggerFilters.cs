using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ApiGateway.API.Swagger;

public sealed class GatewaySchemaFilter : ISchemaFilter
{
    private static readonly string[] EventTypes =
    [
        "PatientRequestCreated", "AiTriageUrgencyDetermined", "DiagnosisConfirmed", "DiagnosisRevised",
        "PrescriptionIssued", "PrescriptionRevoked", "LabResultReceived", "TreatmentStarted",
        "TreatmentCompleted", "AllergyRecorded", "VitalSignRecorded", "ImmunizationRecorded",
        "DocumentUploaded", "ConsultationStarted", "ConsultationJoined", "ConsultationConsent",
        "ConsultationMessage", "ConsultationProtocolDraft", "ConsultationCompleted",
        "ConsultationCancelled", "ConsultationEmergency", "ConsultationExpired",
        "PrescriptionSigned", "PrescriptionFulfilled"
    ];

    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema.Properties is null)
            return;

        if (context.Type.Name is "AppendEventRequestDto" or "AppendEventRequest")
        {
            if (schema.Properties.TryGetValue("eventType", out var eventType) ||
                schema.Properties.TryGetValue("EventType", out eventType))
            {
                eventType.Description = "Тип медицинского события. Алиас document → DocumentUploaded.";
                eventType.Enum = EventTypes.Select(t => (IOpenApiAny)new OpenApiString(t)).ToList();
                eventType.Example = new OpenApiString("DiagnosisConfirmed");
            }

            if (schema.Properties.TryGetValue("payloadJson", out var payload) ||
                schema.Properties.TryGetValue("PayloadJson", out payload))
            {
                payload.Description = "JSON-строка с телом события, например {\"icd10\":\"J06.9\"}.";
                payload.Example = new OpenApiString("{\"icd10\":\"J06.9\"}");
            }
        }

        if (context.Type.Name is "LoginRequestDto" or "LoginRequest")
        {
            if (schema.Properties.TryGetValue("preferredProfileType", out var preferred) ||
                schema.Properties.TryGetValue("PreferredProfileType", out preferred))
            {
                preferred.Description = "Patient | Doctor | Organization — активирует профиль перед выдачей JWT. По умолчанию Patient.";
                preferred.Enum =
                [
                    new OpenApiString("Patient"),
                    new OpenApiString("Doctor"),
                    new OpenApiString("Organization")
                ];
            }
        }

        if (context.Type.Name is "CreateConsultationRequestDto" or "CreateConsultationRequest")
        {
            if (schema.Properties.TryGetValue("consultationType", out var ctype) ||
                schema.Properties.TryGetValue("ConsultationType", out ctype))
            {
                ctype.Description =
                    "SyncChat | Video | Async | InPerson | HomeVisit. " +
                    "Алиасы: chat/text → SyncChat, video/audio → Video, async/AsyncChat → Async.";
                ctype.Enum =
                [
                    new OpenApiString("SyncChat"),
                    new OpenApiString("Video"),
                    new OpenApiString("Async"),
                    new OpenApiString("InPerson"),
                    new OpenApiString("HomeVisit")
                ];
                ctype.Example = new OpenApiString("SyncChat");
            }
        }
    }
}

public sealed class GatewayOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (context.ApiDescription.RelativePath?.Contains("medical-records") == true &&
            context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true &&
            context.ApiDescription.RelativePath.Contains("history"))
        {
            var eventTypesParam = operation.Parameters?.FirstOrDefault(p =>
                string.Equals(p.Name, "eventTypes", StringComparison.OrdinalIgnoreCase));
            if (eventTypesParam is not null)
            {
                eventTypesParam.Description =
                    "Фильтр типов через запятую. Пример: DiagnosisConfirmed,DocumentUploaded. " +
                    "Алиас document → DocumentUploaded. Без параметра возвращаются все события.";
                eventTypesParam.Schema.Example = new OpenApiString("DocumentUploaded");
            }
        }

        if (context.ApiDescription.RelativePath?.StartsWith("api/v1/doctors") == true &&
            context.ApiDescription.HttpMethod?.Equals("GET", StringComparison.OrdinalIgnoreCase) == true &&
            !context.ApiDescription.RelativePath.Contains("schedule"))
        {
            var queryParam = operation.Parameters?.FirstOrDefault(p =>
                string.Equals(p.Name, "query", StringComparison.OrdinalIgnoreCase));
            if (queryParam is not null)
                queryParam.Description = "Поиск по ФИО (имя, отчество, фамилия), регистронезависимо.";

            var specializationParam = operation.Parameters?.FirstOrDefault(p =>
                string.Equals(p.Name, "specialization", StringComparison.OrdinalIgnoreCase));
            if (specializationParam is not null)
                specializationParam.Description = "Фильтр по специализации врача (подстрока).";
        }
    }
}
