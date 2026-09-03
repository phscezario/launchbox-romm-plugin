using RommPlugin.Core.Helpers;
using Xunit;

namespace RommPlugin.Tests.Helpers
{
    public class SecureCredentialStorageTests
    {
        [Fact]
        public void Encrypt_ReturnsEmpty_WhenInputEmpty()
        {
            var result = SecureCredentialStorage.Encrypt("");

            Assert.Equal("", result);
        }

        [Fact]
        public void Encrypt_ReturnsNull_WhenInputNull()
        {
            var result = SecureCredentialStorage.Encrypt(null);

            Assert.Null(result);
        }

        [Fact]
        public void Decrypt_ReturnsEmpty_WhenInputEmpty()
        {
            var result = SecureCredentialStorage.Decrypt("");

            Assert.Equal("", result);
        }

        [Fact]
        public void Decrypt_ReturnsNull_WhenInputNull()
        {
            var result = SecureCredentialStorage.Decrypt(null);

            Assert.Null(result);
        }

        [Fact]
        public void EncryptDecrypt_RoundTrip()
        {
            var plainText = "my-secret-password-123";

            var encrypted = SecureCredentialStorage.Encrypt(plainText);
            var decrypted = SecureCredentialStorage.Decrypt(encrypted);

            Assert.NotEqual(plainText, encrypted);
            Assert.Equal(plainText, decrypted);
        }

        [Fact]
        public void IsEncrypted_ReturnsFalse_WhenEmpty()
        {
            Assert.False(SecureCredentialStorage.IsEncrypted(""));
            Assert.False(SecureCredentialStorage.IsEncrypted(null));
        }

        [Fact]
        public void IsEncrypted_ReturnsTrue_WhenValidBase64()
        {
            var encrypted = SecureCredentialStorage.Encrypt("test");

            var result = SecureCredentialStorage.IsEncrypted(encrypted);

            Assert.True(result);
        }

        [Fact]
        public void IsEncrypted_ReturnsFalse_WhenPlainText()
        {
            var result = SecureCredentialStorage.IsEncrypted("not-base64!@#$%");

            Assert.False(result);
        }

        [Fact]
        public void Decrypt_ReturnsOriginal_WhenNotEncrypted()
        {
            var result = SecureCredentialStorage.Decrypt("not-encrypted-value");

            Assert.Equal("not-encrypted-value", result);
        }
    }
}
