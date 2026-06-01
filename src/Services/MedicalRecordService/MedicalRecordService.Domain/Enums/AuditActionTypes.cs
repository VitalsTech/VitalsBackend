namespace MedicalRecordService.Domain.Enums;

public static class AuditActionTypes
{
    public const string Read = "Read";
    public const string Create = "Create";
    public const string AccessGrantCreated = "AccessGrantCreated";
    public const string AccessGrantRevoked = "AccessGrantRevoked";
}
