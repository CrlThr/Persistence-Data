using System;
using System.IO;
using System.Security.Cryptography;

public static class CryptoService
{
    public static byte[] DeriveKey(string password, byte[] salt, int keySize = 32, int iterations = 100_000)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(keySize);
    }

    public static (byte[] ciphertext, byte[] nonce, byte[] tag) Encrypt(byte[] plaintext, byte[] key)
    {
        int nonceSize = 12;
        int tagSize = 16;
        using var aes = new AesGcm(key, tagSize);
        byte[] nonce = new byte[nonceSize];
        RandomNumberGenerator.Fill(nonce);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[tagSize];
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return (ciphertext, nonce, tag);
    }

    public static byte[] Decrypt(byte[] ciphertext, byte[] nonce, byte[] tag, byte[] key)
    {
        int tagSize = tag.Length;
        using var aes = new AesGcm(key, tagSize);
        byte[] plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return plaintext;
    }
}
