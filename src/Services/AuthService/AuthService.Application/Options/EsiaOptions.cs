namespace AuthService.Application.Options;

public sealed class EsiaOptions
{
    public const string SectionName = "Esia";

    public bool Enabled { get; set; }
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string AuthorizationEndpoint { get; set; } = "https://esia.gosuslugi.ru/aas/oauth2/v2/ac";
    public string TokenEndpoint { get; set; } = "https://esia.gosuslugi.ru/aas/oauth2/v3/te";
    public string UserInfoEndpoint { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = string.Empty;
    public string Scope { get; set; } = "openid fullname mobile snils email";
}
