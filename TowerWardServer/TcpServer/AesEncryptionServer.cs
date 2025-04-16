using System;
using System.Text;
using System.Security.Cryptography;

namespace TcpServer
{
    /// <summary>
    /// Provides AES encryption and decryption utilities for securing messages
    /// between the server and clients after the initial RSA handshake.
    /// </summary>
    public static class AesEncryptionServer
    {
        /// <summary>
        /// Encrypts the specified plaintext using AES with the given key and IV,
        /// then returns the ciphertext as a Base64‐encoded string.
        /// </summary>
        /// <param name="plainText">The UTF‑8 text to encrypt.</param>
        /// <param name="key">The AES key bytes.</param>
        /// <param name="iv">The AES initialization vector bytes.</param>
        /// <returns>A Base64 string representing the encrypted data.</returns>
        public static string EncryptAES(string plainText, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor())
                {
                    byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                    byte[] encrypted = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                    return Convert.ToBase64String(encrypted);
                }
            }
        }

        /// <summary>
        /// Decrypts the specified Base64‑encoded ciphertext using AES with the given key and IV,
        /// then returns the result as a UTF‑8 plaintext string.
        /// </summary>
        /// <param name="cipherBase64">The Base64 string containing AES‑encrypted data.</param>
        /// <param name="key">The AES key bytes.</param>
        /// <param name="iv">The AES initialization vector bytes.</param>
        /// <returns>The decrypted plaintext string.</returns>
        public static string DecryptAES(string cipherBase64, byte[] key, byte[] iv)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor())
                {
                    byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
}
