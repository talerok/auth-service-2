using System.Security.Cryptography;
using System.Text;

namespace Auth.Infrastructure;

internal static class TwoFactorOtpSecurity
{
    public static string CreateSalt() => Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));

    public static string HashOtp(string otp, string salt)
    {
        var data = Encoding.UTF8.GetBytes($"{salt}:{otp}");
        return Convert.ToBase64String(SHA256.HashData(data));
    }

    public static bool VerifyOtp(string otp, string salt, string expectedHash)
    {
        try
        {
            var actualHashBytes = Convert.FromBase64String(HashOtp(otp, salt));
            var expectedHashBytes = Convert.FromBase64String(expectedHash);
            return CryptographicOperations.FixedTimeEquals(actualHashBytes, expectedHashBytes);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public static string EncryptOtp(string otp, string keyMaterial)
    {
        var key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(otp);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];

        using var aes = new AesGcm(key, 16);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);

        var payload = new byte[nonce.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(nonce, 0, payload, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, payload, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, payload, nonce.Length + ciphertext.Length, tag.Length);
        return Convert.ToBase64String(payload);
    }

    public static string DecryptOtp(string encryptedOtp, string keyMaterial)
    {
        var payload = Convert.FromBase64String(encryptedOtp);
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
