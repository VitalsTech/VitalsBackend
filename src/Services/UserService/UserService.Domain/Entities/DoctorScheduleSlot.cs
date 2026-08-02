using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Domain.Entities;

public class DoctorScheduleSlot
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid DoctorProfileId { get; set; }

    [ForeignKey(nameof(DoctorProfileId))]
    public DoctorProfile DoctorProfile { get; set; } = null!;

    public DateTime StartsAt { get; set; }
    public DateTime EndsAt { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsOnline { get; set; } = true;

    /// <summary>PublicId пациента, забронировавшего слот.</summary>
    public Guid? PatientId { get; set; }

    /// <summary>Консультация, созданная под эту бронь.</summary>
    public Guid? ConsultationSessionId { get; set; }

    public DateTime? BookedAt { get; set; }
}
