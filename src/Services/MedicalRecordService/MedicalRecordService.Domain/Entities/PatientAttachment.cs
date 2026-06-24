namespace MedicalRecordService.Domain.Entities;

public sealed class PatientAttachment
{
    public Guid Id { get; set; }
    public Guid PatientId { get; set; }
    public Guid UploadedByUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string ObjectKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? CdnUrl { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
}
