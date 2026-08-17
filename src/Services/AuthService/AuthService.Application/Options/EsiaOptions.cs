namespace AuthService.Application.Options;

public sealed class EsiaOptions
{
    public const string SectionName = "Esia";

    /// <summary>Master switch. Production ignores this unless <see cref="AllowProduction"/> is true.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// DEV-заглушка без портала ЕСИА. true на Development/Docker.
    /// </summary>
    public bool UseStub { get; set; }

    /// <summary>Never enable the test portal against real users. Keep false in Production.</summary>
    public bool AllowProduction { get; set; }

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>PFX/P12 сертификата ИС, которым подписывается client_secret (тот же, что загружен в ЕСИА).</summary>
    public string CertificatePath { get; set; } = string.Empty;
    public string CertificatePassword { get; set; } = string.Empty;

    /// <summary>
    /// Fingerprint сертификата (HEX), как в технологическом портале / cpverify ГОСТ.
    /// Если пусто — SHA256 от DER сертификата.
    /// </summary>
    public string CertificateHash { get; set; } = string.Empty;

    /// <summary>Must match the callback registered in ESIA (Gateway URL).</summary>
    public string RedirectUri { get; set; } = "http://localhost:5080/api/v1/auth/esia/callback";

    public string AuthorizationEndpoint { get; set; } = "https://esia-portal1.test.gosuslugi.ru/aas/oauth2/v2/ac";
    public string TokenEndpoint { get; set; } = "https://esia-portal1.test.gosuslugi.ru/aas/oauth2/v3/te";
    public string RestBaseUrl { get; set; } = "https://esia-portal1.test.gosuslugi.ru";

    /// <summary>
    /// Scopes for FIO, contacts, SNILS, documents (ОМС), addresses.
    /// Clinics/doctors are not requested.
    /// </summary>
    public string Scope { get; set; } = "openid fullname birthdate gender snils inn id_doc medical_doc email mobile addresses";

    public int StateTtlMinutes { get; set; } = 10;

    /// <summary>Allowed prefixes for post-login returnUrl (web + mobile deep links).</summary>
    public string[] AllowedReturnUrlPrefixes { get; set; } =
    [
        "http://localhost",
        "https://localhost",
        "http://127.0.0.1",
        "vitals://",
        "exp://"
    ];
}
