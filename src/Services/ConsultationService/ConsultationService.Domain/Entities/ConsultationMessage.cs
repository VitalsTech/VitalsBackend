using ConsultationService.Domain.Enums;

namespace ConsultationService.Domain.Entities;

public sealed class ConsultationMessage
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public long SequenceNumber { get; set; }
    public Guid SenderId { get; set; }
    public ParticipantRole SenderRole { get; set; }
    public MessageType MessageType { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public bool IsImportant { get; set; }
    public DateTime SentAt { get; set; }
    public DateTime? ReadAt { get; set; }

    public ConsultationSession? Session { get; set; }
}
