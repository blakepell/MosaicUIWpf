/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace BbsNavigator.Common
{
    /// <summary>
    /// Inspects SSH private key files without decrypting them.
    /// </summary>
    /// <remarks>
    /// The inspection only reads the key's envelope so the app can decide whether a passphrase
    /// prompt is needed before a connection is attempted. Key material is never parsed here.
    /// </remarks>
    internal static class SshKeyFileInspector
    {
        private const string OpenSshHeader = "-----BEGIN OPENSSH PRIVATE KEY-----";
        private const string OpenSshFooter = "-----END OPENSSH PRIVATE KEY-----";
        private static readonly byte[] OpenSshMagic = "openssh-key-v1\0"u8.ToArray();

        /// <summary>
        /// Determines whether a private key file is protected by a passphrase.
        /// </summary>
        /// <param name="path">The full path of the private key file.</param>
        /// <returns>
        /// <see langword="true"/> when the key is encrypted and a passphrase is required;
        /// otherwise, <see langword="false"/>. An unreadable or unrecognized file reports
        /// <see langword="false"/> so the connection attempt, not the inspection, surfaces the error.
        /// </returns>
        public static bool IsEncrypted(string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                return false;
            }

            string text;

            try
            {
                text = File.ReadAllText(path);
            }
            catch (Exception)
            {
                return false;
            }

            // PKCS#8 states encryption in the PEM label itself.
            if (text.Contains("BEGIN ENCRYPTED PRIVATE KEY", StringComparison.Ordinal))
            {
                return true;
            }

            // Classic OpenSSL PEM keys carry "Proc-Type: 4,ENCRYPTED" in the PEM headers.
            if (text.Contains("Proc-Type:", StringComparison.Ordinal) &&
                text.Contains("ENCRYPTED", StringComparison.Ordinal))
            {
                return true;
            }

            return IsEncryptedOpenSshKey(text);
        }

        /// <summary>
        /// Reads the cipher name from an OpenSSH v1 key envelope. The body is
        /// <c>"openssh-key-v1\0"</c> followed by a length-prefixed cipher name, which is
        /// <c>none</c> for an unencrypted key.
        /// </summary>
        /// <param name="text">The full text of the key file.</param>
        /// <returns><see langword="true"/> when the key names a cipher other than <c>none</c>.</returns>
        private static bool IsEncryptedOpenSshKey(string text)
        {
            int start = text.IndexOf(OpenSshHeader, StringComparison.Ordinal);

            if (start < 0)
            {
                return false;
            }

            start += OpenSshHeader.Length;
            int end = text.IndexOf(OpenSshFooter, start, StringComparison.Ordinal);
            string body = end < 0 ? text[start..] : text[start..end];
            byte[] blob;

            try
            {
                blob = Convert.FromBase64String(body.Replace("\r", string.Empty).Replace("\n", string.Empty));
            }
            catch (FormatException)
            {
                return false;
            }

            if (blob.Length < OpenSshMagic.Length + sizeof(uint) ||
                !blob.AsSpan(0, OpenSshMagic.Length).SequenceEqual(OpenSshMagic))
            {
                return false;
            }

            int offset = OpenSshMagic.Length;
            uint cipherLength = BinaryPrimitives.ReadUInt32BigEndian(blob.AsSpan(offset, sizeof(uint)));
            offset += sizeof(uint);

            if (cipherLength == 0 || cipherLength > (uint)(blob.Length - offset))
            {
                return false;
            }

            string cipher = Encoding.ASCII.GetString(blob, offset, (int)cipherLength);
            return !string.Equals(cipher, "none", StringComparison.Ordinal);
        }
    }
}
