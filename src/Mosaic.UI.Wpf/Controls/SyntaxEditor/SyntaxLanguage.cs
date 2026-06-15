/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The set of languages for which <see cref="SyntaxEditor"/> ships bundled, theme-aware
    /// syntax highlighting definitions.
    /// </summary>
    public enum SyntaxLanguage
    {
        /// <summary>
        /// No syntax highlighting (plain text).
        /// </summary>
        None,

        /// <summary>
        /// JavaScript Object Notation (.json).
        /// </summary>
        Json,

        /// <summary>
        /// C# source (.cs).
        /// </summary>
        CSharp,

        /// <summary>
        /// XML and XML-derived formats (.xml, .xaml, .config, .csproj, ...).
        /// </summary>
        Xml
    }

    /// <summary>
    /// Maps <see cref="SyntaxLanguage"/> values to their embedded resource base names and resolves
    /// the appropriate language from a file path or extension.
    /// </summary>
    public static class SyntaxLanguageMap
    {
        /// <summary>
        /// Gets the resource base name (the portion before the theme suffix) for a language, e.g.
        /// <see cref="SyntaxLanguage.CSharp"/> maps to <c>"CSharp"</c>. Returns <c>null</c> for
        /// <see cref="SyntaxLanguage.None"/>.
        /// </summary>
        /// <param name="language">The language to map.</param>
        public static string? GetResourceBaseName(SyntaxLanguage language)
        {
            return language switch
            {
                SyntaxLanguage.Json => "Json",
                SyntaxLanguage.CSharp => "CSharp",
                SyntaxLanguage.Xml => "Xml",
                _ => null
            };
        }

        /// <summary>
        /// Resolves a <see cref="SyntaxLanguage"/> from a file path or file extension. The argument
        /// may be a full path (e.g. <c>C:\temp\data.json</c>), a file name, or a bare extension
        /// (with or without the leading dot, e.g. <c>.cs</c> or <c>cs</c>). Returns
        /// <see cref="SyntaxLanguage.None"/> when the extension is unrecognized.
        /// </summary>
        /// <param name="pathOrExtension">The path or extension to inspect.</param>
        public static SyntaxLanguage FromExtension(string? pathOrExtension)
        {
            if (string.IsNullOrWhiteSpace(pathOrExtension))
            {
                return SyntaxLanguage.None;
            }

            // Normalize to a leading-dot extension regardless of whether a full path or bare token was passed.
            string ext = pathOrExtension.Contains('.')
                ? System.IO.Path.GetExtension(pathOrExtension)
                : "." + pathOrExtension;

            return ext.ToLowerInvariant() switch
            {
                ".json" => SyntaxLanguage.Json,
                ".cs" => SyntaxLanguage.CSharp,
                ".xml" or ".xaml" or ".config" or ".csproj" or ".xsd" or ".xsl" or ".xslt"
                    or ".manifest" or ".targets" or ".props" or ".nuspec" or ".wsdl" => SyntaxLanguage.Xml,
                _ => SyntaxLanguage.None
            };
        }
    }
}
