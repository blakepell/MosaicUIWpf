/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using System.Security.Cryptography;
using System.Text.Json;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Protects portable BBS credential records with a user-supplied passphrase.
    /// </summary>
    /// <remarks>
    /// The passphrase is not persisted. A 256-bit AES key is derived with PBKDF2-HMAC-SHA256
    /// using a random salt and the credential payload is authenticated and encrypted with AES-GCM.
    /// </remarks>
    internal static class CredentialProtector
    {
        private const int IterationCount = 600_000;
        private const int SaltSize = 16;
        private const int NonceSize = 12;
        private const int TagSize = 16;
        private const int KeySize = 32;
        private static readonly byte[] Header = "BNC1"u8.ToArray();
        private static readonly byte[] AssociatedData = "BbsNavigator.Credentials.v1"u8.ToArray();
        private const string VerifierUserName = "BbsNavigator.MasterPassphrase";
        private const string VerifierPassword = "CredentialVerifier.v1";

        /// <summary>
        /// Creates an encrypted verifier for the application-wide credential passphrase.
        /// </summary>
        /// <param name="passphrase">The application-wide encryption passphrase.</param>
        /// <returns>A protected verifier suitable for settings persistence.</returns>
        public static string CreatePassphraseVerifier(string passphrase)
        {
            return Protect(new BbsCredentials(VerifierUserName, VerifierPassword), passphrase);
        }

        /// <summary>
        /// Verifies an application-wide credential passphrase against a protected verifier.
        /// </summary>
        /// <param name="protectedVerifier">The protected verifier stored in application settings.</param>
        /// <param name="passphrase">The application-wide encryption passphrase.</param>
        /// <returns>
        /// <see langword="true"/> if the passphrase authenticates the verifier; otherwise,
        /// <see langword="false"/>.
        /// </returns>
        public static bool VerifyPassphrase(string protectedVerifier, string passphrase)
        {
            return TryUnprotect(protectedVerifier, passphrase, out BbsCredentials? verifier) &&
                   verifier is { UserName: VerifierUserName, Password: VerifierPassword };
        }

        /// <summary>
        /// Encrypts a credential record with a user-supplied passphrase.
        /// </summary>
        /// <param name="credentials">The credential values to protect.</param>
        /// <param name="passphrase">The portable encryption passphrase.</param>
        /// <returns>A versioned Base64 credential envelope suitable for JSON persistence.</returns>
        /// <exception cref="ArgumentException">
        /// <paramref name="passphrase"/> is empty or contains only white-space characters.
        /// </exception>
        public static string Protect(BbsCredentials credentials, string passphrase)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);

            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            byte[] nonce = RandomNumberGenerator.GetBytes(NonceSize);
            byte[] plaintext = JsonSerializer.SerializeToUtf8Bytes(credentials);
            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[TagSize];
            byte[] key = Rfc2898DeriveBytes.Pbkdf2(
                passphrase,
                salt,
                IterationCount,
                HashAlgorithmName.SHA256,
                KeySize);

            try
            {
                using var aes = new AesGcm(key, TagSize);
                aes.Encrypt(nonce, plaintext, ciphertext, tag, AssociatedData);

                byte[] envelope = new byte[Header.Length + SaltSize + NonceSize + TagSize + ciphertext.Length];
                int offset = 0;
                Header.CopyTo(envelope, offset);
                offset += Header.Length;
                salt.CopyTo(envelope, offset);
                offset += SaltSize;
                nonce.CopyTo(envelope, offset);
                offset += NonceSize;
                tag.CopyTo(envelope, offset);
                offset += TagSize;
                ciphertext.CopyTo(envelope, offset);
                return Convert.ToBase64String(envelope);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(key);
                CryptographicOperations.ZeroMemory(plaintext);
            }
        }

        /// <summary>
        /// Attempts to decrypt a credential record with a user-supplied passphrase.
        /// </summary>
        /// <param name="protectedValue">The versioned Base64 credential envelope.</param>
        /// <param name="passphrase">The portable encryption passphrase.</param>
        /// <param name="credentials">
        /// When this method returns, contains the decrypted credentials when successful; otherwise,
        /// <see langword="null"/>. This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the credential record was authenticated and decrypted;
        /// otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TryUnprotect(string protectedValue, string passphrase, out BbsCredentials? credentials)
        {
            credentials = null;
            byte[]? key = null;
            byte[]? plaintext = null;

            try
            {
                byte[] envelope = Convert.FromBase64String(protectedValue);
                int fixedLength = Header.Length + SaltSize + NonceSize + TagSize;

                if (envelope.Length <= fixedLength ||
                    !envelope.AsSpan(0, Header.Length).SequenceEqual(Header))
                {
                    return false;
                }

                int offset = Header.Length;
                ReadOnlySpan<byte> salt = envelope.AsSpan(offset, SaltSize);
                offset += SaltSize;
                ReadOnlySpan<byte> nonce = envelope.AsSpan(offset, NonceSize);
                offset += NonceSize;
                ReadOnlySpan<byte> tag = envelope.AsSpan(offset, TagSize);
                offset += TagSize;
                ReadOnlySpan<byte> ciphertext = envelope.AsSpan(offset);

                key = Rfc2898DeriveBytes.Pbkdf2(
                    passphrase,
                    salt,
                    IterationCount,
                    HashAlgorithmName.SHA256,
                    KeySize);
                plaintext = new byte[ciphertext.Length];

                using var aes = new AesGcm(key, TagSize);
                aes.Decrypt(nonce, ciphertext, tag, plaintext, AssociatedData);
                credentials = JsonSerializer.Deserialize<BbsCredentials>(plaintext);
                return credentials is { UserName: not null, Password: not null };
            }
            catch (Exception ex) when (ex is FormatException or CryptographicException or JsonException or ArgumentException)
            {
                return false;
            }
            finally
            {
                if (key != null)
                {
                    CryptographicOperations.ZeroMemory(key);
                }

                if (plaintext != null)
                {
                    CryptographicOperations.ZeroMemory(plaintext);
                }
            }
        }
    }
}
