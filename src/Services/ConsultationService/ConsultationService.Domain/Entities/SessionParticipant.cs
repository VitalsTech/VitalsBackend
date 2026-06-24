using ConsultationService.Domain.Enums;

namespace ConsultationService.Domain.Entities;

public sealed class SessionParticipant
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid UserId { get; set; }
    public ParticipantRole Role { get; set; }
    public DateTime JoinedAt { get; set; }
}
