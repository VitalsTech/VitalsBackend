using System.Security.Cryptography;
using AuthService.Application.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthService.Infrastructure.Security;

public sealed class RsaKeyProvider
{
    public RsaSecurityKey SigningKey { get; }
    public string KeyId { get; }

    public RsaKeyProvider(IOptions<JwtOptions> options)
    {
        var jwt = options.Value;
        KeyId = jwt.KeyId;

        if (!string.IsNullOrWhiteSpace(jwt.RsaPrivateKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(jwt.RsaPrivateKeyPem);
            SigningKey = new RsaSecurityKey(rsa) { KeyId = KeyId };
            return;
        }

        var generated = RSA.Create(2048);
        SigningKey = new RsaSecurityKey(generated) { KeyId = KeyId };
    }
}
