using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace UserService.Application.Services
{
    public interface IEncryptionService
    {
        string Encrypt(string plainText);
        string Decrypt(string cipherText);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly byte[] _key;
        private readonly byte[] _iv;

        public EncryptionService(IOptions<EncryptionSettings> settings)
        {
            // TODO: Ключ должен быть 32 байта для AES-256
            _key = Encoding.UTF8.GetBytes(settings.Value.Key.PadRight(32).Substring(0, 32));
            _iv = Encoding.UTF8.GetBytes(settings.Value.IV.PadRight(16).Substring(0, 16));
        }

        public string Encrypt(string plainText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            return Convert.ToBase64String(cipherBytes);
        }

        public string Decrypt(string cipherText)
        {
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = _iv;

            var decryptor = aes.CreateDecryptor();
            var cipherBytes = Convert.FromBase64String(cipherText);
            var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);

            return Encoding.UTF8.GetString(plainBytes);
        }
    }

    public class EncryptionSettings
    {
        public string Key { get; set; } = string.Empty;
        public string IV { get; set; } = string.Empty;
    }
}