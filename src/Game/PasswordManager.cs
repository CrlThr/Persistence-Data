using System;
using System.Security.Cryptography;
using System.Text;

public static class PasswordService
{
    private const int SaltSize = 16;        // 128 bits
    private const int HashSize = 32;        // 256 bits
    private const int Iterations = 100_000; // nombre d'itérations PBKDF2

    // Génère le hash et le sel à partir du mot de passe
    public static (string hashB64, string saltB64) HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] hash = pbkdf2.GetBytes(HashSize);

        return (Convert.ToBase64String(hash), Convert.ToBase64String(salt));
    }

    // Vérifie si un mot de passe correspond au hash stocké
    public static bool VerifyPassword(string password, string storedHashB64, string storedSaltB64)
    {
        byte[] salt = Convert.FromBase64String(storedSaltB64);
        byte[] storedHash = Convert.FromBase64String(storedHashB64);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
        byte[] computedHash = pbkdf2.GetBytes(HashSize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }
}
