namespace MedicalRecordService.Domain.Entities;

public class AccessGrant
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PatientId { get; set; }
    public Guid GranteeId { get; set; }
    public string GranteeType { get; set; } = "Doctor";
    public string ScopesJson { get; set; } = "[]";
    public string? RestrictedCategoriesJson { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public Guid GrantedByUserId { get; set; }
}
