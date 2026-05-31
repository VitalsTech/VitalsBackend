namespace UserService.Application.DTOs.Common
{
    public class UserWithProfilesDto
    {
        public Guid PublicId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? SecondName { get; set; }
        public string Surename { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public string Sex { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<ProfileInfoDto> Profiles { get; set; } = new();
        public Guid? ActiveProfileId { get; set; }
    }

    public class ProfileInfoDto
    {
        public Guid ProfileId { get; set; }
        public string ProfileType { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public object? Data { get; set; }
    }
}