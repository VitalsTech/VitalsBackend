namespace MedicalRecordService.Domain.Enums;

public static class AccessScopes
{
    public const string ReadHistory = "read:history";
    public const string ReadProjections = "read:projections";
    public const string WriteEvents = "write:events";
    public const string ManageConsents = "manage:consents";
}
