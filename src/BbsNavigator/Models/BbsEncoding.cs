/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Text;

namespace BbsNavigator.Models
{
    /// <summary>
    /// Identifies the character encoding used to interpret BBS terminal text.
    /// </summary>
    public enum BbsEncoding
    {
        /// <summary>
        /// IBM PC code page 437, the encoding classic DOS-era bulletin boards use for
        /// box-drawing and block-graphic art.
        /// </summary>
        [Description("IBM PC / DOS (CP437)")]
        Cp437,

        /// <summary>
        /// UTF-8, used by modern telnet services, MUDs, and retro systems that have been
        /// updated for Unicode.
        /// </summary>
        [Description("UTF-8")]
        Utf8,

        /// <summary>
        /// ISO-8859-1 (Latin-1), common on European systems from the early internet era.
        /// </summary>
        [Description("Latin-1 (ISO 8859-1)")]
        Latin1
    }

    /// <summary>
    /// Provides conversions from <see cref="BbsEncoding"/> to <see cref="Encoding"/>.
    /// </summary>
    public static class BbsEncodingExtensions
    {
        /// <summary>
        /// Returns the <see cref="Encoding"/> for the specified value, falling back to
        /// Latin-1 if the CP437 code page provider is unavailable.
        /// </summary>
        /// <param name="encoding">The encoding selection.</param>
        /// <returns>The matching <see cref="Encoding"/> instance.</returns>
        public static Encoding ToEncoding(this BbsEncoding encoding)
        {
            switch (encoding)
            {
                case BbsEncoding.Cp437:
                    try
                    {
                        return Encoding.GetEncoding(437);
                    }
                    catch (NotSupportedException)
                    {
                        return Encoding.Latin1;
                    }
                case BbsEncoding.Latin1:
                    return Encoding.Latin1;
                default:
                    return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
            }
        }
    }
}
