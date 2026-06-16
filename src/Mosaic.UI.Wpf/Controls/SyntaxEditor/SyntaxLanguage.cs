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
    /// Defines the line comment markers used by <see cref="SyntaxEditor"/> comment commands.
    /// </summary>
    /// <param name="LinePrefix">The marker inserted after line indentation.</param>
    /// <param name="LineSuffix">The optional marker appended to the line.</param>
    public sealed record SyntaxCommentDefinition(string LinePrefix, string? LineSuffix = null);

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
        Xml,

        /// <summary>
        /// JavaScript source (.js, .mjs, .cjs, .jsx).
        /// </summary>
        JavaScript,

        /// <summary>
        /// Structured Query Language (.sql).
        /// </summary>
        Sql,

        /// <summary>
        /// Markdown documents (.md, .markdown).
        /// </summary>
        Markdown,

        /// <summary>
        /// C source and header files (.c, .h).
        /// </summary>
        C,

        /// <summary>
        /// Lua source (.lua).
        /// </summary>
        Lua,

        /// <summary>
        /// Python source (.py, .pyw, .pyi).
        /// </summary>
        Python,

        /// <summary>
        /// INI and related configuration files (.ini, .cfg, .conf, .properties).
        /// </summary>
        Ini,

        /// <summary>
        /// Java source (.java).
        /// </summary>
        Java,

        /// <summary>
        /// Swift source (.swift).
        /// </summary>
        Swift,

        /// <summary>
        /// Classic BASIC source (.bas).
        /// </summary>
        Basic,

        /// <summary>
        /// Visual Basic .NET source (.vb).
        /// </summary>
        VbNet,

        /// <summary>
        /// Perl source and module files (.pl, .pm, .t, .pod).
        /// </summary>
        Perl,

        /// <summary>
        /// PHP source and template files (.php, .phtml, .php3, .php4, .php5, .phps).
        /// </summary>
        Php
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
                SyntaxLanguage.JavaScript => "JavaScript",
                SyntaxLanguage.Sql => "Sql",
                SyntaxLanguage.Markdown => "Markdown",
                SyntaxLanguage.C => "C",
                SyntaxLanguage.Lua => "Lua",
                SyntaxLanguage.Python => "Python",
                SyntaxLanguage.Ini => "Ini",
                SyntaxLanguage.Java => "Java",
                SyntaxLanguage.Swift => "Swift",
                SyntaxLanguage.Basic => "Basic",
                SyntaxLanguage.VbNet => "VbNet",
                SyntaxLanguage.Perl => "Perl",
                SyntaxLanguage.Php => "Php",
                _ => null
            };
        }

        /// <summary>
        /// Gets the line comment markers for a language. Returns <c>null</c> when the language does
        /// not have a built-in line-oriented comment command definition.
        /// </summary>
        /// <param name="language">The language to map.</param>
        public static SyntaxCommentDefinition? GetLineCommentDefinition(SyntaxLanguage language)
        {
            return language switch
            {
                SyntaxLanguage.Json or SyntaxLanguage.CSharp or SyntaxLanguage.JavaScript or SyntaxLanguage.C
                    or SyntaxLanguage.Java or SyntaxLanguage.Swift or SyntaxLanguage.Php => new SyntaxCommentDefinition("//"),
                SyntaxLanguage.Python or SyntaxLanguage.Perl => new SyntaxCommentDefinition("#"),
                SyntaxLanguage.Lua or SyntaxLanguage.Sql => new SyntaxCommentDefinition("--"),
                SyntaxLanguage.Ini => new SyntaxCommentDefinition(";"),
                SyntaxLanguage.Basic => new SyntaxCommentDefinition("REM"),
                SyntaxLanguage.VbNet => new SyntaxCommentDefinition("'"),
                SyntaxLanguage.Xml => new SyntaxCommentDefinition("<!--", "-->"),
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
                ".js" or ".mjs" or ".cjs" or ".jsx" => SyntaxLanguage.JavaScript,
                ".sql" => SyntaxLanguage.Sql,
                ".md" or ".markdown" => SyntaxLanguage.Markdown,
                ".c" or ".h" => SyntaxLanguage.C,
                ".lua" => SyntaxLanguage.Lua,
                ".py" or ".pyw" or ".pyi" => SyntaxLanguage.Python,
                ".ini" or ".cfg" or ".conf" or ".properties" => SyntaxLanguage.Ini,
                ".java" => SyntaxLanguage.Java,
                ".swift" => SyntaxLanguage.Swift,
                ".bas" => SyntaxLanguage.Basic,
                ".vb" => SyntaxLanguage.VbNet,
                ".pl" or ".pm" or ".t" or ".pod" => SyntaxLanguage.Perl,
                ".php" or ".phtml" or ".php3" or ".php4" or ".php5" or ".phps" => SyntaxLanguage.Php,
                ".xml" or ".xaml" or ".config" or ".csproj" or ".xsd" or ".xsl" or ".xslt"
                    or ".manifest" or ".targets" or ".props" or ".nuspec" or ".wsdl" => SyntaxLanguage.Xml,
                _ => SyntaxLanguage.None
            };
        }
    }
}
