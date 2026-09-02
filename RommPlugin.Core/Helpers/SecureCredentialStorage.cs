using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RommPlugin.Core.Logging;

namespace RommPlugin.Core.Helpers
{
    /// <summary>
    /// Provides DPAPI-based encryption and decryption for sensitive credentials (passwords, API tokens).
    /// Encryption is machine-specific and user-specific, using Windows Data Protection API.
    /// </summary>
    public static class SecureCredentialStorage
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("RomM-LaunchBox-Plugin-Credentials");

        /// <summary>
        /// Encrypts a plain text string using DPAPI with the current user's credentials.
        /// </summary>
        /// <param name="plainText">The plain text string to encrypt.</param>
        /// <returns>The encrypted string in Base64 format, or the original string if encryption fails.</returns>
        public static string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var plainBytes = Encoding.UTF8.GetBytes(plainText);
                var encryptedBytes = ProtectedData.Protect(plainBytes, Entropy, DataProtectionScope.CurrentUser);
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                RommLogger.LogError($"Failed to encrypt credential: {ex.Message}");
                return plainText;
            }
        }

        /// <summary>
        /// Decrypts a DPAPI-encrypted string back to plain text.
        /// </summary>
        /// <param name="cipherText">The Base64-encoded encrypted string to decrypt.</param>
        /// <returns>The decrypted plain text, or the original string if decryption fails.</returns>
        public static string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                var cipherBytes = Convert.FromBase64String(cipherText);
                var plainBytes = ProtectedData.Unprotect(cipherBytes, Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            catch
            {
                return cipherText;
            }
        }

        /// <summary>
        /// Determines whether a string appears to be DPAPI-encrypted (valid Base64 format).
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if the string appears to be encrypted; false otherwise.</returns>
        public static bool IsEncrypted(string value)
        {
            if (string.IsNullOrEmpty(value))
                return false;

            try
            {
                Convert.FromBase64String(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
