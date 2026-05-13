using System.ComponentModel.DataAnnotations;
using UserService.Domain.Enums;

namespace UserService.Domain.Entities
{
    public class User
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid PublicId { get; set; } = Guid.NewGuid();

        [Required]
        [Phone]
        public string PhoneNumber { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string FirstName { get; set; } = string.Empty;

        public string? SecondName { get; set; }

        [Required]
        public string Surename { get; set; } = string.Empty;

        public DateTime BirthDate { get; set; }

        public Sex Sex { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }

        public ICollection<Profile> Profiles { get; set; } = new List<Profile>();

        public void UpdateTimestamp()
        {
            UpdatedAt = DateTime.UtcNow;
        }
    }
}