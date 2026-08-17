using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using AuthService.Application.Exceptions;
using AuthService.Application.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Esia;

internal sealed class EsiaRequestSigner
{
    private readonly EsiaOptions _options;
    private readonly Lazy<X509Certificate2?> _certificate;

    public EsiaRequestSigner(EsiaOptions options)
    {
        _options = options;
        _certificate = new Lazy<X509Certificate2?>(LoadCertificate);
    }

    public static string CreateTimestamp() =>
        DateTime.UtcNow.ToString("yyyy.MM.dd HH:mm:ss +0000");

    public bool CanSign => _certificate.Value?.HasPrivateKey == true;

    public string GetCertificateHash()
    {
        if (!string.IsNullOrWhiteSpace(_options.CertificateHash))
            return _options.CertificateHash.Trim().Replace(" ", "").ToUpperInvariant();

        var cert = _certificate.Value
            ?? throw new AuthValidationException(MissingCertMessage);
        return Convert.ToHexString(SHA256.HashData(cert.RawData));
    }

    public string SignAuthorization(string timestamp, string state) =>
        Sign($"{_options.ClientId}{_options.Scope}{timestamp}{state}{_options.RedirectUri}");

    public string SignToken(string timestamp, string state, string code) =>
        Sign($"{_options.ClientId}{_options.Scope}{timestamp}{state}{_options.RedirectUri}{code}");

    public static string ToQueryString(IReadOnlyDictionary<string, string> parameters)
    {
        // ЕСИА: пробел → +, «+» в timezone → %2B, «:» → %3A (не %20).
        return string.Join("&", parameters.Select(kv =>
            $"{kv.Key}={EncodeValue(kv.Value)}"));
    }

    private string Sign(string message)
    {
        var cert = _certificate.Value
            ?? throw new AuthValidationException(MissingCertMessage);
        if (!cert.HasPrivateKey)
            throw new AuthValidationException("В PFX нет закрытого ключа — ЕСИА не сможет проверить client_secret.");

        try
        {
            var content = new ContentInfo(Encoding.UTF8.GetBytes(message));
            var signedCms = new SignedCms(content, detached: true);
            var signer = new CmsSigner(cert)
            {
                IncludeOption = X509IncludeOption.EndCertOnly
            };
            signedCms.ComputeSignature(signer, silent: true);
            return Base64UrlEncoder.Encode(signedCms.Encode());
        }
        catch (CryptographicException ex)
        {
            throw new AuthValidationException(
                "Не удалось подписать запрос ЕСИА. Для ГОСТ нужен сертификат ГОСТ Р 34.10-2012 " +
                "(и обычно КриптоПро); RSA PFX работает через PKCS#7. " + ex.Message);
        }
    }

    private X509Certificate2? LoadCertificate()
    {
        if (string.IsNullOrWhiteSpace(_options.CertificatePath))
            return null;
        if (!File.Exists(_options.CertificatePath))
            throw new AuthValidationException($"Файл сертификата ЕСИА не найден: {_options.CertificatePath}");

        return new X509Certificate2(
            File.ReadAllBytes(_options.CertificatePath),
            _options.CertificatePassword,
            X509KeyStorageFlags.Exportable);
    }

    private static string EncodeValue(string value) =>
        Uri.EscapeDataString(value).Replace("%20", "+");

    internal const string MissingCertMessage =
        "ЕСИА (v2/ac) не принимает мнемонику без подписи. " +
        "Нужен тот же сертификат, что загружен в ИС VITALS: ESIA_CERT_PATH (файл .pfx) " +
        "и желательно ESIA_CERT_HASH (отпечаток из технологического портала или cpverify). " +
        "ESIA_CLIENT_SECRET — не пароль, это вычисляемая подпись.";
}
