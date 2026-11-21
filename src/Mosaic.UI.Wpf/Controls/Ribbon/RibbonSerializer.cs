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

using System.Xml;
using System.Xml.Linq;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The outcome of restoring Ribbon preferences.
    /// </summary>
    public sealed class RibbonRestoreResult
    {
        /// <summary>Gets a value indicating whether the input was readable and its preferences were applied.</summary>
        public bool Success { get; internal set; }

        /// <summary>Gets the schema version found in the input, or 0 when unreadable.</summary>
        public int Version { get; internal set; }

        /// <summary>Gets diagnostics for skipped values (unknown commands, invalid enums, duplicates, newer schema).</summary>
        public List<string> Diagnostics { get; } = new();
    }

    /// <summary>
    /// Saves and restores Ribbon user preferences as versioned XML: layout mode, density, minimized state, the
    /// selected tab and the Quick Access Toolbar (location, mode, labels and ordered command keys). Hover, focus,
    /// open popups and temporary overflow decisions are never persisted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Restore after the Ribbon's commands exist (for example in the window's <c>Loaded</c> handler). Input is
    /// fully parsed and validated before anything is applied, so malformed input leaves the Ribbon unchanged.
    /// </para>
    /// <para>
    /// Preferences files are treated as untrusted input: DTDs are rejected, external resources are never resolved,
    /// and documents larger than <see cref="MaxDocumentLength"/> characters are ignored.
    /// </para>
    /// </remarks>
    public static class RibbonSerializer
    {
        /// <summary>The schema version written by <see cref="Serialize"/>.</summary>
        public const int CurrentVersion = 1;

        /// <summary>The largest preferences document, in characters, that <see cref="Deserialize"/> and <see cref="LoadFromFile"/> accept.</summary>
        public const int MaxDocumentLength = 1024 * 1024;

        private const string RootName = "RibbonPreferences";

        /// <summary>
        /// Serializes the selected preferences to XML.
        /// </summary>
        public static string Serialize(Ribbon ribbon, RibbonSerializerOptions options = RibbonSerializerOptions.All)
        {
            ArgumentNullException.ThrowIfNull(ribbon);

            var root = new XElement(RootName, new XAttribute("version", CurrentVersion));

            if (options.HasFlag(RibbonSerializerOptions.LayoutMode))
            {
                root.Add(new XElement("LayoutMode", ribbon.LayoutMode));
            }

            if (options.HasFlag(RibbonSerializerOptions.UserInterfaceDensity))
            {
                root.Add(new XElement("Density", ribbon.Density));
            }

            if (options.HasFlag(RibbonSerializerOptions.MinimizedState))
            {
                root.Add(new XElement("Minimized", ribbon.IsMinimized));
            }

            if (options.HasFlag(RibbonSerializerOptions.SelectedTab) && ribbon.SelectedTab?.LayoutKey is { Length: > 0 } tabKey)
            {
                root.Add(new XElement("SelectedTab", new XAttribute("key", tabKey)));
            }

            if ((options & RibbonSerializerOptions.QuickAccessToolbar) != 0)
            {
                var qat = new XElement("QuickAccessToolbar");
                if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarLocation))
                {
                    qat.Add(new XAttribute("location", ribbon.QuickAccessToolbarLocation));
                }

                if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarMode))
                {
                    qat.Add(new XAttribute("mode", ribbon.QuickAccessToolbarMode));
                }

                if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarAllowLabels))
                {
                    qat.Add(new XAttribute("showLabels", ribbon.QuickAccessToolbarShowLabels));
                }

                if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarItems))
                {
                    var items = new XElement("Items");
                    foreach (var key in ribbon.QuickAccessToolbarKeys)
                    {
                        items.Add(new XElement("Command", new XAttribute("key", key)));
                    }

                    qat.Add(items);
                }

                root.Add(qat);
            }

            return new XDocument(root).ToString();
        }

        /// <summary>
        /// Restores the selected preferences from XML.
        /// </summary>
        public static RibbonRestoreResult Deserialize(Ribbon ribbon, string? xml, RibbonSerializerOptions options = RibbonSerializerOptions.All)
        {
            ArgumentNullException.ThrowIfNull(ribbon);
            var result = new RibbonRestoreResult();

            if (string.IsNullOrWhiteSpace(xml))
            {
                result.Diagnostics.Add("No Ribbon preferences were provided; defaults are kept.");
                return result;
            }

            if (xml.Length > MaxDocumentLength)
            {
                result.Diagnostics.Add($"Ribbon preferences exceed {MaxDocumentLength} characters and were ignored.");
                return result;
            }

            XElement root;
            try
            {
                root = ParseSafely(xml).Root ?? throw new XmlException("Missing root element.");
            }
            catch (XmlException ex)
            {
                result.Diagnostics.Add($"Ribbon preferences are malformed and were ignored: {ex.Message}");
                return result;
            }

            if (root.Name.LocalName != RootName)
            {
                result.Diagnostics.Add($"Unexpected root element '{root.Name.LocalName}'; preferences were ignored.");
                return result;
            }

            if (!int.TryParse((string?)root.Attribute("version"), out var version) || version < 1)
            {
                result.Diagnostics.Add("Ribbon preferences have no valid version; preferences were ignored.");
                return result;
            }

            result.Version = version;
            if (version > CurrentVersion)
            {
                result.Diagnostics.Add($"Ribbon preferences use schema version {version}, newer than the supported {CurrentVersion}; preferences were ignored.");
                return result;
            }

            // Parse and validate everything first; apply only after validation (coordinated restoration).
            var layoutMode = Parse<RibbonLayoutMode>(root.Element("LayoutMode")?.Value, "LayoutMode", result);
            var density = Parse<UserInterfaceDensity>(root.Element("Density")?.Value, "Density", result);
            var minimized = ParseBool(root.Element("Minimized")?.Value, "Minimized", result);
            var selectedTab = ((string?)root.Element("SelectedTab")?.Attribute("key"))?.Trim();

            var qat = root.Element("QuickAccessToolbar");
            var location = Parse<QuickAccessToolbarLocation>((string?)qat?.Attribute("location"), "QuickAccessToolbar location", result);
            var mode = Parse<QuickAccessToolbarMode>((string?)qat?.Attribute("mode"), "QuickAccessToolbar mode", result);
            var showLabels = ParseBool((string?)qat?.Attribute("showLabels"), "QuickAccessToolbar showLabels", result);

            List<string>? keys = null;
            if (qat?.Element("Items") is { } itemsElement)
            {
                keys = new List<string>();
                foreach (var command in itemsElement.Elements("Command"))
                {
                    var key = ((string?)command.Attribute("key"))?.Trim();
                    if (string.IsNullOrEmpty(key))
                    {
                        result.Diagnostics.Add("A Quick Access Toolbar entry without a key was skipped.");
                    }
                    else if (keys.Contains(key))
                    {
                        result.Diagnostics.Add($"Duplicate Quick Access Toolbar key '{key}' was skipped.");
                    }
                    else
                    {
                        keys.Add(key);
                    }
                }
            }

            ribbon.CloseTransientSurfaces();

            if (options.HasFlag(RibbonSerializerOptions.LayoutMode) && layoutMode.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.LayoutModeProperty, layoutMode.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.UserInterfaceDensity) && density.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.DensityProperty, density.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.MinimizedState) && minimized.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.IsMinimizedProperty, minimized.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarMode) && mode.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.QuickAccessToolbarModeProperty, mode.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarLocation) && location.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.QuickAccessToolbarLocationProperty, location.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarAllowLabels) && showLabels.HasValue)
            {
                ribbon.SetCurrentValue(Ribbon.QuickAccessToolbarShowLabelsProperty, showLabels.Value);
            }

            if (options.HasFlag(RibbonSerializerOptions.QuickAccessToolbarItems) && keys != null)
            {
                // Restoring items must not override a restored Hidden mode (adding normally reveals the toolbar).
                var restoredMode = ribbon.QuickAccessToolbarMode;
                ribbon.RestoreQuickAccessToolbar(keys, result.Diagnostics);
                ribbon.SetCurrentValue(Ribbon.QuickAccessToolbarModeProperty, restoredMode);
            }

            if (options.HasFlag(RibbonSerializerOptions.SelectedTab) && !string.IsNullOrEmpty(selectedTab) && !ribbon.SelectTab(selectedTab))
            {
                result.Diagnostics.Add($"Selected tab '{selectedTab}' was skipped: it is unknown, hidden or disabled.");
            }

            result.Success = true;
            return result;
        }

        /// <summary>
        /// Saves preferences to a file using atomic replacement, so a crash mid-write never leaves a truncated file.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="ribbon"/> or <paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
        public static void SaveToFile(Ribbon ribbon, string path, RibbonSerializerOptions options = RibbonSerializerOptions.All)
        {
            ArgumentNullException.ThrowIfNull(ribbon);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            var xml = Serialize(ribbon, options);
            var fullPath = Path.GetFullPath(path);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // A unique temporary name avoids clobbering a concurrent save; the finally removes it if the swap fails.
            var temp = $"{fullPath}.{Guid.NewGuid():N}.tmp";
            try
            {
                File.WriteAllText(temp, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                if (File.Exists(fullPath))
                {
                    File.Replace(temp, fullPath, null, ignoreMetadataErrors: true);
                }
                else
                {
                    File.Move(temp, fullPath);
                }
            }
            finally
            {
                if (File.Exists(temp))
                {
                    File.Delete(temp);
                }
            }
        }

        /// <summary>
        /// Restores preferences from a file. A missing, unreadable or oversized file keeps the defaults, and the
        /// reason is reported in <see cref="RibbonRestoreResult.Diagnostics"/>.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="ribbon"/> or <paramref name="path"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty or whitespace.</exception>
        public static RibbonRestoreResult LoadFromFile(Ribbon ribbon, string path, RibbonSerializerOptions options = RibbonSerializerOptions.All)
        {
            ArgumentNullException.ThrowIfNull(ribbon);
            ArgumentException.ThrowIfNullOrWhiteSpace(path);

            string? xml = null;
            try
            {
                var file = new FileInfo(path);
                if (file.Exists)
                {
                    // UTF-8 never uses fewer bytes than characters, so a byte count within the limit is a safe precheck.
                    if (file.Length > MaxDocumentLength * 4L)
                    {
                        var tooLarge = new RibbonRestoreResult();
                        tooLarge.Diagnostics.Add($"Ribbon preferences file is too large ({file.Length} bytes) and was ignored.");
                        return tooLarge;
                    }

                    xml = File.ReadAllText(file.FullName);
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or System.Security.SecurityException)
            {
                var failed = new RibbonRestoreResult();
                failed.Diagnostics.Add($"Ribbon preferences could not be read: {ex.Message}");
                return failed;
            }

            return Deserialize(ribbon, xml, options);
        }

        private static XDocument ParseSafely(string xml)
        {
            var settings = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxDocumentLength,
                MaxCharactersFromEntities = 1024,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true
            };

            using var reader = XmlReader.Create(new StringReader(xml), settings);
            return XDocument.Load(reader);
        }

        private static T? Parse<T>(string? value, string name, RibbonRestoreResult result) where T : struct, Enum
        {
            if (value == null)
            {
                return null;
            }

            if (Enum.TryParse<T>(value.Trim(), ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            {
                return parsed;
            }

            result.Diagnostics.Add($"Invalid {name} value '{value}' was ignored.");
            return null;
        }

        private static bool? ParseBool(string? value, string name, RibbonRestoreResult result)
        {
            if (value == null)
            {
                return null;
            }

            if (bool.TryParse(value.Trim(), out var parsed))
            {
                return parsed;
            }

            result.Diagnostics.Add($"Invalid {name} value '{value}' was ignored.");
            return null;
        }
    }
}
