namespace AuthService.Application.Options;

public sealed class UserServiceOptions
{
    public const string SectionName = "UserService";

    public string BaseUrl { get; set; } = "http://localhost:5195";
}
