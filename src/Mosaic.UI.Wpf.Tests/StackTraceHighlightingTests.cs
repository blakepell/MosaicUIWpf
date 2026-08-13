/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Verifies the bundled stack trace syntax definitions classify frames the way the regular
    /// expressions intend. These are easy to break: AvalonEdit only applies rules to the text
    /// between span boundaries, so a lookahead that crosses one silently stops matching.
    /// </summary>
    public class StackTraceHighlightingTests
    {
        /// <summary>
        /// Loads a bundled stack trace definition from the library's embedded resources.
        /// </summary>
        /// <param name="theme">Either <c>"Light"</c> or <c>"Dark"</c>.</param>
        /// <returns>The parsed highlighting definition.</returns>
        private static IHighlightingDefinition Load(string theme)
        {
            string resourceName = $"Mosaic.UI.Wpf.Assets.SyntaxEditor.StackTrace.{theme}.xshd";
            using var stream = typeof(Mosaic.UI.Wpf.Controls.SyntaxEditor).Assembly.GetManifestResourceStream(resourceName);
            Assert.NotNull(stream);

            using var reader = XmlReader.Create(stream);
            return HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }

        /// <summary>
        /// Maps every colored span of a single line to its color name.
        /// </summary>
        /// <param name="definition">The highlighting definition to apply.</param>
        /// <param name="line">The line of text to highlight.</param>
        /// <returns>A lookup of matched text to the color name that was applied.</returns>
        private static Dictionary<string, string> Classify(IHighlightingDefinition definition, string line)
        {
            var document = new TextDocument(line);
            var highlighter = new DocumentHighlighter(document, definition);
            var result = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var section in highlighter.HighlightLine(1).Sections)
            {
                result[document.GetText(section.Offset, section.Length)] = section.Color?.Name ?? string.Empty;
            }

            return result;
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void DefinitionLoads(string theme)
        {
            Assert.Equal("StackTrace", Load(theme).Name);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void FrameSeparatesTypesFromMethods(string theme)
        {
            var colors = Classify(Load(theme), "   at NWebsec.AspNetCore.Middleware.Middleware.MiddlewareBase.Invoke(HttpContext context)");

            Assert.Equal("Keyword", colors["at"]);
            Assert.Equal("Namespace", colors["NWebsec"]);
            Assert.Equal("Namespace", colors["AspNetCore"]);
            Assert.Equal("Type", colors["MiddlewareBase"]);
            Assert.Equal("Method", colors["Invoke"]);
            Assert.Equal("Type", colors["HttpContext"]);
            Assert.Equal("ParameterName", colors["context"]);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void GenericFrameSeparatesTypeArgumentsAndParameters(string theme)
        {
            var colors = Classify(Load(theme), "at Iuf.Mvc.Cache.CacheProvider.GetAsync[T](String cacheKey, MemoryCacheEntryOptions cacheOptions, Func`1 getItemFunc)");

            Assert.Equal("Type", colors["CacheProvider"]);
            Assert.Equal("Method", colors["GetAsync"]);
            Assert.Equal("Type", colors["T"]);
            Assert.Equal("Type", colors["String"]);
            Assert.Equal("Type", colors["MemoryCacheEntryOptions"]);
            Assert.Equal("Type", colors["Func`1"]);
            Assert.Equal("ParameterName", colors["cacheKey"]);
            Assert.Equal("ParameterName", colors["getItemFunc"]);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void FrameHighlightsSourceLocation(string theme)
        {
            var colors = Classify(Load(theme), "   at MyApp.Program.Main(String[] args) in C:\\src\\App\\Program.cs:line 42");

            Assert.Equal("Method", colors["Main"]);
            Assert.Equal("FilePath", colors["C:\\src\\App\\Program.cs"]);
            Assert.Equal("Keyword", colors[":line"]);
            Assert.Equal("Number", colors["42"]);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void ExceptionHeaderSeparatesTypeFromMessage(string theme)
        {
            var colors = Classify(Load(theme), "System.IO.FileNotFoundException: Could not load file or assembly.");

            Assert.Equal("Namespace", colors["System"]);
            Assert.Equal("Namespace", colors["IO"]);
            Assert.Equal("ExceptionType", colors["FileNotFoundException"]);
            Assert.Equal("Message", colors["Could not load file or assembly."]);
        }

        [Theory]
        [InlineData("Light")]
        [InlineData("Dark")]
        public void ExceptionHeaderFallsBackForNonSuffixedTypes(string theme)
        {
            var colors = Classify(Load(theme), "Xunit.Sdk.TrueFailure: Assert.True() failure");

            Assert.Equal("ExceptionType", colors["Xunit.Sdk.TrueFailure"]);
        }
    }
}
