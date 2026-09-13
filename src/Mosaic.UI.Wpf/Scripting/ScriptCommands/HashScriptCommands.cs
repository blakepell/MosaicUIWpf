/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Scripting;
using System.Net;

// ReSharper disable InconsistentNaming
#pragma warning disable CA1822

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands
{
    /// <summary>
    /// Hash Script Commands
    /// </summary>
    [ScriptModule(Name = "hash")]
    public partial class HashScriptCommands
    {
        /// <summary>
        /// Returns the MD5 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        public string MD5(string value)
        {
            return Argus.Cryptography.HashUtilities.MD5Hash(value);
        }

        /// <summary>
        /// Returns the SHA1 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        /// 
        public string SHA1(string value)
        {
            return Argus.Cryptography.HashUtilities.Sha1Hash(value);
        }

        /// <summary>
        /// Returns the SHA256 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        public string SHA256(string value)
        {
            return Argus.Cryptography.HashUtilities.Sha256Hash(value);
        }

        /// <summary>
        /// Returns the SHA384 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        public string SHA384(string value)
        {
            return Argus.Cryptography.HashUtilities.Sha384Hash(value);
        }

        /// <summary>
        /// Returns the SHA512 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        public string SHA512(string value)
        {
            return Argus.Cryptography.HashUtilities.Sha512Hash(value);
        }

        /// <summary>
        /// Returns the CRC32 hash for the given string.
        /// </summary>
        /// <param name="value"></param>
        public uint CRC32(string value)
        {
            return Argus.Cryptography.HashUtilities.CRC32(value, Encoding.UTF8);
        }

        /// <summary>
        /// Encodes a base64 string.
        /// </summary>
        /// <param name="value"></param>
        public string EncodeBase64(string value)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Decodes a base64 string.
        /// </summary>
        /// <param name="value"></param>
        public string DecodeBase64(string value)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(value));
        }

        /// <summary>
        /// Encodes a value for use in a URL.
        /// </summary>
        /// <param name="value"></param>
        public string UrlEncode(string value)
        {
            return WebUtility.UrlEncode(value);
        }

        /// <summary>
        /// Decodes a value for use in a URL.
        /// </summary>
        /// <param name="value"></param>
        public string UrlDecode(string value)
        {
            return WebUtility.UrlDecode(value);
        }
    }
}