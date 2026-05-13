using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UserService.Domain.Entities
{
    public class Profile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User User { get; set; } = null!;

        [Required]
        public ProfileType ProfileType { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public PatientProfile? PatientProfile { get; set; }
        public DoctorProfile? DoctorProfile { get; set; }
        public OrganizationProfile? OrganizationProfile { get; set; }
    }

    public enum ProfileType
    {
        Patient = 1,
        Doctor = 2,
        Organization = 3
    }
}