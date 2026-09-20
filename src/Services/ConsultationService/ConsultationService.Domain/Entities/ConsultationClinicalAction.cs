namespace ConsultationService.Domain.Entities;

public sealed class ConsultationClinicalAction
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = "{}";
    public DateTime CreatedAt { get; set; }
    public Guid CreatedByDoctorId { get; set; }
}
