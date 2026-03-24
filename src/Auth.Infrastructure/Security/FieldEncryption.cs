using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure;

internal static class FieldEncryption
{
    public static string Encrypt(string plaintext, string keyMaterial)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        var ciphertext = new byte[plaintextBytes.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintextBytes, ciphertext, tag);

        var payload = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length + ciphertext.Length, tag.Length);
        return Convert.ToBase64String(payload);
    }

    public static string Decrypt(string encrypted, string keyMaterial)
    {
        var payload = Convert.FromBase64String(encrypted);
        var nonce = payload[..12];
        var tag = payload[^16..];
        var ciphertext = payload[12..^16];
        var plaintext = new byte[ciphertext.Length];
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));

        using var aes = new AesGcm(key, 16);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
