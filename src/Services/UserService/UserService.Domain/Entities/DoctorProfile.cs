using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities
{
    public class DoctorProfile
    {
        [Key]
        [ForeignKey(nameof(Profile))]
        public Guid Id { get; set; }

        public Profile Profile { get; set; } = null!;

        [Required]
        public string Specialization { get; set; } = string.Empty;

        [Required]
        public string DiplomaNumber { get; set; } = string.Empty;
        public string? DiplomaSeries { get; set; }

        [Required]
        public string CertificateNumber { get; set; } = string.Empty;
        public DateTime CertificateExpiryDate { get; set; }

        public Guid? OrganizationId { get; set; }
        public DoctorCategory Category { get; set; }
        public string? AcademicDegree { get; set; }
        public string? Biography { get; set; }

        public double Rating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;

        public string PatientIdsJson { get; set; } = "[]";

        [NotMapped]
        public List<Guid> PatientIds
        {
            get => string.IsNullOrEmpty(PatientIdsJson) ? new List<Guid>() : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(PatientIdsJson) ?? new();
            set => PatientIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        public bool IsCertificateValid() => CertificateExpiryDate > DateTime.UtcNow;

        public void UpdateRating(double newRating)
        {
            if (ReviewCount == 0)
            {
                Rating = newRating;
                ReviewCount = 1;
            }
            else
            {
                var totalScore = Rating * ReviewCount + newRating;
                ReviewCount++;
                Rating = totalScore / ReviewCount;
            }
        }
    }
}