using System;
using System.IO;
using DataPersistence;
using Game.Services;
using Game.Models;
using MongoDB.Driver;

namespace Game
{
    public class AuthManager
    {
        private const int MAX_LOGIN_ATTEMPTS = 3;
        private static MongoDbService? _mongoService;
        private static bool _useFileSystem = false;
        
        public static void Initialize(MongoDbService? mongoService)
        {
            _mongoService = mongoService;
            _useFileSystem = mongoService == null;
        }
        
        public static async Task<PlayerData?> AuthenticateUserAsync()
        {
            if (_useFileSystem)
            {
                return AuthenticateUserFileSystem();
            }
            
            if (_mongoService == null) 
                throw new InvalidOperationException("MongoDbService not initialized");
                
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== DUNGEON EXPLORER - LOGIN ===");
                Console.WriteLine($"Storage: MongoDB");
                Console.WriteLine();
                
                Console.Write("Enter your username: ");
                string? username = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Username cannot be empty. Press any key to try again...");
                    Console.ReadKey();
                    continue;
                }
                
                username = username.Trim();
                var existingProfile = await _mongoService.GetProfileByUsernameAsync(username);
                
                if (existingProfile == null)
                {
                    // Account doesn't exist
                    Console.WriteLine($"No account found for '{username}'.");
                    Console.Write("Would you like to create a new account? (y/n): ");
                    
                    ConsoleKeyInfo key = Console.ReadKey();
                    Console.WriteLine();
                    
                    if (key.Key == ConsoleKey.Y)
                    {
                        return await CreateNewAccountAsync(username);
                    }
                    else
                    {
                        Console.WriteLine("Returning to main menu...");
                        Console.ReadKey();
                        return null; // Return to main menu
                    }
                }
                else
                {
                    // Account exists, attempt login
                    return await AttemptLoginAsync(existingProfile);
                }
            }
        }
        
        private static PlayerData? AuthenticateUserFileSystem()
        {
            while (true)
            {
                Console.Clear();
                Console.WriteLine("=== DUNGEON EXPLORER - LOGIN ===");
                Console.WriteLine($"Storage: Local Files");
                Console.WriteLine();
                
                Console.Write("Enter your username: ");
                string? username = Console.ReadLine();
                
                if (string.IsNullOrWhiteSpace(username))
                {
                    Console.WriteLine("Username cannot be empty. Press any key to try again...");
                    Console.ReadKey();
                    continue;
                }
                
                username = username.Trim();
                string savePath = SaveManager<PlayerData>.FormatSavePath(username);
                
                if (!File.Exists(savePath))
                {
                    // Account doesn't exist
                    Console.WriteLine($"No account found for '{username}'.");
                    Console.Write("Would you like to create a new account? (y/n): ");
                    
                    ConsoleKeyInfo key = Console.ReadKey();
                    Console.WriteLine();
                    
                    if (key.Key == ConsoleKey.Y)
                    {
                        return CreateNewAccountFileSystem(username);
                    }
                    else
                    {
                        Console.WriteLine("Returning to main menu...");
                        Console.ReadKey();
                        return null; // Return to main menu
                    }
                }
                else
                {
                    // Account exists, attempt login
                    return AttemptLoginFileSystem(username);
                }
            }
        }
        
        public static PlayerData? AuthenticateUser()
        {
            return AuthenticateUserAsync().GetAwaiter().GetResult();
        }
        
        private static async Task<PlayerData?> CreateNewAccountAsync(string username)
        {
            if (_mongoService == null) return null;
            
            Console.Clear();
            Console.WriteLine($"=== CREATING ACCOUNT FOR '{username}' ===");
            Console.WriteLine();
            
            string password = GetPasswordInput("Enter password: ");
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Password cannot be empty. Returning to main menu...");
                Console.ReadKey();
                return null;
            }
            
            string confirmPassword = GetPasswordInput("Confirm password: ");
            if (password != confirmPassword)
            {
                Console.WriteLine("Passwords do not match. Returning to main menu...");
                Console.ReadKey();
                return null;
            }
            
            Console.WriteLine("Creating account...");
            
            try
            {
                // Generate password hash and salt
                var (passwordHash, salt) = PasswordService.HashPassword(password);
                
                // Create profile in MongoDB
                var profile = new ProfileDoc
                {
                    Username = username,
                    PasswordHash = passwordHash,
                    Salt = salt,
                    CreatedAt = DateTime.UtcNow,
                    LastPlayed = DateTime.UtcNow
                };
                
                await _mongoService.CreateProfileAsync(profile);
                
                // Create initial player data
                var playerData = new PlayerData(username, passwordHash, salt);
                
                // Save encrypted player data to MongoDB
                await _mongoService.SavePlayerDataAsync(username, playerData, password);
                
                Console.WriteLine("Account created successfully!");
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                
                return playerData;
            }
            catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
            {
                Console.WriteLine("Username already exists. Please try a different username.");
                Console.ReadKey();
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating account: {ex.Message}");
                Console.ReadKey();
                return null;
            }
        }
        
        private static async Task<PlayerData?> AttemptLoginAsync(ProfileDoc profile)
        {
            if (_mongoService == null) return null;
            
            for (int attempts = 0; attempts < MAX_LOGIN_ATTEMPTS; attempts++)
            {
                Console.Clear();
                Console.WriteLine($"=== LOGIN FOR '{profile.Username}' ===");
                Console.WriteLine();
                
                if (attempts > 0)
                {
                    Console.WriteLine($"Incorrect password. {MAX_LOGIN_ATTEMPTS - attempts} attempts remaining.");
                    Console.WriteLine();
                }
                
                string password = GetPasswordInput("Enter password: ");
                
                // Verify password using stored hash
                if (PasswordService.VerifyPassword(password, profile.PasswordHash, profile.Salt))
                {
                    try
                    {
                        // Load encrypted player data from MongoDB
                        var playerData = await _mongoService.LoadPlayerDataAsync(profile.Username, password);
                        
                        if (playerData != null)
                        {
                            Console.WriteLine("Login successful!");
                            Console.WriteLine("Press any key to continue...");
                            Console.ReadKey();
                            
                            // Update last played time
                            profile.LastPlayed = DateTime.UtcNow;
                            await _mongoService.UpdateProfileAsync(profile);
                            
                            return playerData;
                        }
                        else
                        {
                            Console.WriteLine("Error loading player data.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error loading player data: {ex.Message}");
                    }
                }
                else
                {
                    Console.WriteLine("Incorrect password.");
                }
                
                if (attempts < MAX_LOGIN_ATTEMPTS - 1)
                {
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey();
                }
            }
            
            Console.WriteLine("Maximum login attempts exceeded. Returning to main menu...");
            Console.ReadKey();
            return null;
        }
        
        private static string GetPasswordInput(string prompt)
        {
            Console.Write(prompt);
            string password = "";
            ConsoleKeyInfo key;
            
            do
            {
                key = Console.ReadKey(true);
                
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    password += key.KeyChar;
                    Console.Write("*");
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password[..^1];
                    Console.Write("\b \b");
                }
            } while (key.Key != ConsoleKey.Enter);
            
            Console.WriteLine();
            return password;
        }
        
        public static async Task SavePlayerProgressAsync(PlayerData playerData, string password, int newHighScore)
        {
            if (_useFileSystem)
            {
                SavePlayerProgressFileSystem(playerData, password, newHighScore);
                return;
            }
            
            if (_mongoService == null) return;
            
            if (newHighScore > playerData.HighScore)
            {
                playerData.HighScore = newHighScore;
            }
            
            playerData.LastPlayed = DateTime.Now;
            playerData.TotalGamesPlayed++;
            
            await _mongoService.SavePlayerDataAsync(playerData.Name, playerData, password);
        }
        
        public static void SavePlayerProgress(PlayerData playerData, string password, int newHighScore)
        {
            SavePlayerProgressAsync(playerData, password, newHighScore).GetAwaiter().GetResult();
        }
        
        // File System Methods (fallback)
        private static PlayerData? CreateNewAccountFileSystem(string username)
        {
            Console.Clear();
            Console.WriteLine($"=== CREATING ACCOUNT FOR '{username}' ===");
            Console.WriteLine();
            
            string password = GetPasswordInput("Enter password: ");
            if (string.IsNullOrEmpty(password))
            {
                Console.WriteLine("Password cannot be empty. Returning to main menu...");
                Console.ReadKey();
                return null;
            }
            
            string confirmPassword = GetPasswordInput("Confirm password: ");
            if (password != confirmPassword)
            {
                Console.WriteLine("Passwords do not match. Returning to main menu...");
                Console.ReadKey();
                return null;
            }
            
            Console.WriteLine("Creating account...");
            
            // Generate password hash and salt
            var (passwordHash, salt) = PasswordService.HashPassword(password);
            
            // Create new player data
            PlayerData playerData = new PlayerData(username, passwordHash, salt);
            
            // Save the account to file
            SaveManager<PlayerData>.Save(username, playerData, password, salt);
            
            Console.WriteLine("Account created successfully!");
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
            
            return playerData;
        }
        
        private static PlayerData? AttemptLoginFileSystem(string username)
        {
            for (int attempts = 0; attempts < MAX_LOGIN_ATTEMPTS; attempts++)
            {
                Console.Clear();
                Console.WriteLine($"=== LOGIN FOR '{username}' ===");
                Console.WriteLine();
                
                if (attempts > 0)
                {
                    Console.WriteLine($"Incorrect password. {MAX_LOGIN_ATTEMPTS - attempts} attempts remaining.");
                    Console.WriteLine();
                }
                
                string password = GetPasswordInput("Enter password: ");
                
                try
                {
                    // Try to decrypt and load the player data with the provided password
                    string savePath = SaveManager<PlayerData>.FormatSavePath(username);
                    
                    // Read the encrypted file to get the salt
                    string fileContents = File.ReadAllText(savePath);
                    var encObj = System.Text.Json.JsonDocument.Parse(fileContents).RootElement;
                    string? saltB64 = encObj.GetProperty("salt").GetString();
                    
                    if (string.IsNullOrEmpty(saltB64))
                    {
                        Console.WriteLine("Corrupted save file - missing salt.");
                        continue;
                    }
                    
                    // Load the full player data with password verification
                    PlayerData playerData = SaveManager<PlayerData>.Load(username, password, saltB64);
                    
                    // If we got here without exception, the decryption worked
                    // Verify the password using stored hash as additional check
                    if (!string.IsNullOrEmpty(playerData.PasswordHash) && 
                        !string.IsNullOrEmpty(playerData.Salt) &&
                        PasswordService.VerifyPassword(password, playerData.PasswordHash, playerData.Salt))
                    {
                        Console.WriteLine("Login successful!");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                        
                        // Update last played time
                        playerData.LastPlayed = DateTime.Now;
                        SaveManager<PlayerData>.Save(username, playerData, password, playerData.Salt);
                        
                        return playerData;
                    }
                    else
                    {
                        Console.WriteLine("Incorrect password or corrupted data.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Authentication failed: {ex.Message}");
                }
                
                if (attempts < MAX_LOGIN_ATTEMPTS - 1)
                {
                    Console.WriteLine("Press any key to try again...");
                    Console.ReadKey();
                }
            }
            
            Console.WriteLine("Maximum login attempts exceeded. Returning to main menu...");
            Console.ReadKey();
            return null;
        }
        
        private static void SavePlayerProgressFileSystem(PlayerData playerData, string password, int newHighScore)
        {
            if (newHighScore > playerData.HighScore)
            {
                playerData.HighScore = newHighScore;
            }
            
            playerData.LastPlayed = DateTime.Now;
            playerData.TotalGamesPlayed++;
            
            SaveManager<PlayerData>.Save(playerData.Name, playerData, password, playerData.Salt);
        }
    }
}