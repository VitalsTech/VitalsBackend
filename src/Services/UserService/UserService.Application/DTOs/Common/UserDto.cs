namespace UserService.Application.DTOs.Common
{
    public class UserDto
    {
        public Guid PublicId { get; set; }
        public string? Email { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string Surename { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Sex { get; set; } = string.Empty;
        public string? InsuranceNumber { get; set; }
        public string? SNILS { get; set; }
        public AddressDto? RegistrationAddress { get; set; }
        public AddressDto? ActualAddress { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string UserType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? BlockedUntil { get; set; }
        public string? BlockReason { get; set; }
    }
}