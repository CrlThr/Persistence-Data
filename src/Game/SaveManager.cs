using System;
using System.IO;
using System.Text.Json;

namespace DataPersistence
{
    class SaveManager<T> where T : new()
    {
        public SaveManager()
        {
            if (!Directory.Exists("Saves"))
                Directory.CreateDirectory("Saves");
        }

        public static T Load(string name, string? password = null, string? saltB64 = null)
        {
            string savePath = SaveManager<T>.FormatSavePath(name);
            if (!File.Exists(savePath))
            {
                return new T();
            }

            string contents = File.ReadAllText(savePath);
            if (string.IsNullOrWhiteSpace(contents))
                return new T();

            // If no password, fallback to plain JSON
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(saltB64))
            {
                var obj = JsonSerializer.Deserialize<T>(contents);
                return obj == null ? new T() : obj;
            }

            // Encrypted file: parse JSON with ciphertext, nonce, tag, salt
            var encObj = JsonDocument.Parse(contents).RootElement;
            string? ciphertextB64 = encObj.GetProperty("ciphertext").GetString();
            string? nonceB64 = encObj.GetProperty("nonce").GetString();
            string? tagB64 = encObj.GetProperty("tag").GetString();
            string? saltB64File = encObj.GetProperty("salt").GetString();

            if (ciphertextB64 == null || nonceB64 == null || tagB64 == null || saltB64File == null)
                return new T();

            byte[] ciphertext = Convert.FromBase64String(ciphertextB64);
            byte[] nonce = Convert.FromBase64String(nonceB64);
            byte[] tag = Convert.FromBase64String(tagB64);
            byte[] salt = Convert.FromBase64String(saltB64File);

            // Use salt from file for key derivation
            byte[] key = CryptoService.DeriveKey(password, salt);
            byte[] plaintext = CryptoService.Decrypt(ciphertext, nonce, tag, key);
            string json = System.Text.Encoding.UTF8.GetString(plaintext);

            var decryptedObj = JsonSerializer.Deserialize<T>(json);
            return decryptedObj == null ? new T() : decryptedObj;
        }

        public static T LoadFromFile(string path, bool createNew)
        {
            string contents = File.ReadAllText(path);
            if (contents.Length == 0)
                return new T();

            // Try to parse as encrypted, fallback to plain JSON
            try
            {
                var encObj = JsonDocument.Parse(contents).RootElement;
                string? ciphertextB64 = encObj.GetProperty("ciphertext").GetString();
                string? nonceB64 = encObj.GetProperty("nonce").GetString();
                string? tagB64 = encObj.GetProperty("tag").GetString();
                string? saltB64 = encObj.GetProperty("salt").GetString();

                // Can't decrypt without password, return empty
                return new T();
            }
            catch
            {
                var obj = JsonSerializer.Deserialize<T>(contents);
                return obj == null ? new T() : obj;
            }
        }

        public static void Save(string name, T data, string? password = null, string? saltB64 = null)
        {
            string savePath = SaveManager<T>.FormatSavePath(name);
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });

            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(saltB64))
            {
                File.WriteAllText(savePath, json);
                return;
            }

            // Encrypt
            byte[] salt = Convert.FromBase64String(saltB64);
            byte[] key = CryptoService.DeriveKey(password, salt);
            byte[] plaintext = System.Text.Encoding.UTF8.GetBytes(json);
            var (ciphertext, nonce, tag) = CryptoService.Encrypt(plaintext, key);

            var encObj = new
            {
                ciphertext = Convert.ToBase64String(ciphertext),
                nonce = Convert.ToBase64String(nonce),
                tag = Convert.ToBase64String(tag),
                salt = saltB64
            };

            string encJson = JsonSerializer.Serialize(encObj, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(savePath, encJson);
        }

        public static string FormatSavePath(string name)
        {
            return $"Saves/{name}.json";
        }
    }
}
