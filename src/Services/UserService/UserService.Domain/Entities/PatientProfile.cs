using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities
{
    public class PatientProfile
    {
        [Key]
        [ForeignKey(nameof(Profile))]
        public Guid Id { get; set; }

        public Profile Profile { get; set; } = null!;

        public string? InsuranceNumber { get; set; }
        public string? SNILS { get; set; }           
        public BloodType? BloodType { get; set; }
        public string? Allergies { get; set; }

        public string DoctorIdsJson { get; set; } = "[]";
        public string OrganizationIdsJson { get; set; } = "[]";

        [NotMapped]
        public List<Guid> DoctorIds
        {
            get => string.IsNullOrEmpty(DoctorIdsJson) ? new List<Guid>() : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(DoctorIdsJson) ?? new();
            set => DoctorIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }

        [NotMapped]
        public List<Guid> OrganizationIds
        {
            get => string.IsNullOrEmpty(OrganizationIdsJson) ? new List<Guid>() : System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(OrganizationIdsJson) ?? new();
            set => OrganizationIdsJson = System.Text.Json.JsonSerializer.Serialize(value);
        }
    }
}