namespace PrescriptionService.Application.Options;

public sealed class JwtValidationOptions
{
    public const string SectionName = "Jwt";
    public string Issuer { get; set; } = "vitals-auth";
    public string Audience { get; set; } = "vitals-api";
    public string? JwksUrl { get; set; }
}

public sealed class MedicalRecordServiceOptions
{
    public const string SectionName = "MedicalRecordService";
    public string BaseUrl { get; set; } = "http://localhost:5210";
}

public sealed class UserServiceOptions
{
    public const string SectionName = "UserService";
    public string BaseUrl { get; set; } = "http://localhost:5195";
}

public sealed class IntegrationServiceOptions
{
    public const string SectionName = "IntegrationService";
    public string BaseUrl { get; set; } = "http://localhost:5270";
    public bool UseStub { get; set; } = true;
}

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";
    public bool Enabled { get; set; }
    public string BootstrapServers { get; set; } = "localhost:9092";
    public string ClientId { get; set; } = "prescription-service";
    public string PrescriptionCreatedTopic { get; set; } = "prescription.created";
    public string PrescriptionStatusUpdatedTopic { get; set; } = "prescription.status_updated";
    public string PrescriptionExpiringSoonTopic { get; set; } = "prescription.expiring_soon";
    public string PrescriptionExpiredTopic { get; set; } = "prescription.expired";
    public string PrescriptionFulfilledTopic { get; set; } = "prescription.fulfilled";
}

public sealed class PrescriptionOptions
{
    public const string SectionName = "Prescription";
    public int DefaultValidityDays { get; set; } = 60;
    public int ControlledValidityDays { get; set; } = 30;
    public int PreferentialValidityDays { get; set; } = 365;
    public int ExpiringSoonDays { get; set; } = 3;
    public string QrSigningSecret { get; set; } = "dev-prescription-qr-secret-change-in-prod";
}

public sealed class ESignatureOptions
{
    public const string SectionName = "ESignature";
    public bool UseStub { get; set; } = true;
}
