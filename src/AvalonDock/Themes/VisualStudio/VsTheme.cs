using System;
using System.IO;
using System.Windows;

namespace AvalonDock.Themes
{
    /// <summary>
    /// Base class for themes that load their colors from a .vstheme file.
    /// Extends <see cref="DictionaryTheme"/> to build the <see cref="ResourceDictionary"/>
    /// at runtime from parsed VS theme colors.
    /// </summary>
    public class VsTheme : DictionaryTheme
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VsTheme"/> class
        /// from a .vstheme file stream.
        /// </summary>
        /// <param name="stream">The stream containing the .vstheme XML content.</param>
        public VsTheme(Stream stream)
            : base(BuildFromStream(stream))
        {
        }

        private static ResourceDictionary BuildFromStream(Stream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            var palette = VsThemeParser.Parse(stream);
            return VsThemeResourceBuilder.Build(palette);
        }
    }
}
