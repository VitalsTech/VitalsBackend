using System.Security.Cryptography;
using ApiGateway.Application.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ApiGateway.Infrastructure.Security;

public sealed class JwtSigningKeyProvider
{
    public SecurityKey SigningKey { get; }

    public JwtSigningKeyProvider(
        IOptions<JwtOptions> options,
        IHttpClientFactory httpClientFactory,
        IHostEnvironment environment,
        ILogger<JwtSigningKeyProvider> logger)
    {
        var jwt = options.Value;

        if (!string.IsNullOrWhiteSpace(jwt.RsaPublicKeyPem))
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(jwt.RsaPublicKeyPem);
            SigningKey = new RsaSecurityKey(rsa);
            return;
        }

        if (!string.IsNullOrWhiteSpace(jwt.JwksUrl))
        {
            try
            {
                var client = httpClientFactory.CreateClient("jwks");
                var json = client.GetStringAsync(jwt.JwksUrl).GetAwaiter().GetResult();
                var set = new JsonWebKeySet(json);
                SigningKey = set.GetSigningKeys().FirstOrDefault()
                    ?? throw new InvalidOperationException("JWKS does not contain a signing key.");
                return;
            }
            catch (Exception ex) when (environment.IsDevelopment())
            {
                logger.LogWarning(ex, "Could not load JWKS from {Url}; using ephemeral dev key. Start AuthService for real tokens.", jwt.JwksUrl);
            }
        }

        if (environment.IsDevelopment())
        {
            SigningKey = new RsaSecurityKey(RSA.Create(2048));
            return;
        }

        throw new InvalidOperationException("Configure Jwt:RsaPublicKeyPem or Jwt:JwksUrl.");
    }
}
