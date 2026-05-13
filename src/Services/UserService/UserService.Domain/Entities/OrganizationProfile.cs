using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UserService.Domain.Enums;
using System.Net;

namespace UserService.Domain.Entities
{
    public class OrganizationProfile
    {
        [Key]
        [ForeignKey(nameof(Profile))]
        public Guid Id { get; set; }

        public Profile Profile { get; set; } = null!;

        [Required]
        public string LegalName { get; set; } = string.Empty;

        [Required]
        public string DisplayName { get; set; } = string.Empty;

        [Required]
        public string INN { get; set; } = string.Empty;
        public string? KPP { get; set; }

        [Required]
        public string OGRN { get; set; } = string.Empty;

        public Address? LegalAddress { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }

        [Required]
        public OrganizationRole Role { get; set; }

        public Guid? AdministratorId { get; set; }
    }
}