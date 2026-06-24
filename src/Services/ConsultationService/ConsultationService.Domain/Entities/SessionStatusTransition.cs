using ConsultationService.Domain.Enums;

namespace ConsultationService.Domain.Entities;

public sealed class SessionStatusTransition
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public ConsultationStatus FromStatus { get; set; }
    public ConsultationStatus ToStatus { get; set; }
    public string Initiator { get; set; } = string.Empty;
    public Guid? InitiatorUserId { get; set; }
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; }
}
