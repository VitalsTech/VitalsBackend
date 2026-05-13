namespace UserService.Application.DTOs.Common
{
    public class CreateUserResponse
    {
        public Guid PublicId { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
