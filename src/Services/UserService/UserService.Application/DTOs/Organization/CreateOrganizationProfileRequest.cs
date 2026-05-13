using UserService.Domain.Entities;

namespace UserService.Application.DTOs.Organization
{
    public class CreateOrganizationProfileRequest
    {
        public string LegalName { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string INN { get; set; } = string.Empty;
        public string? KPP { get; set; }
        public string OGRN { get; set; } = string.Empty;
        public Address? LegalAddress { get; set; }
        public string? ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid? AdministratorId { get; set; }
    }
}