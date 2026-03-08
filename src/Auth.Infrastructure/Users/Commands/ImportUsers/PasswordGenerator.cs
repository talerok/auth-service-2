using System.Security.Cryptography;

namespace Auth.Infrastructure.Users.Commands.ImportUsers;

internal static class PasswordGenerator
{
    internal static string GenerateTemporaryPassword()
    {
        const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%&*";
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);
        return string.Create(16, bytes.ToArray(), (span, b) =>
        {
            for (var i = 0; i < span.Length; i++)
                span[i] = chars[b[i] % chars.Length];
        });
    }
}
