using System;
using System.Windows;

namespace AvalonDock.Themes
{
    /// <summary>
    /// Represents the theme.
    /// </summary>
    public abstract class Theme : DependencyObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Theme"/> class.
        /// </summary>
        public Theme()
        {
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        public string Name => GetType().Name.Replace("Theme", string.Empty);

        Uri ResourceUri => GetResourceUri();

        /// <summary>
        /// Gets the get Resource Uri.
        /// </summary>
        /// <returns>The requested value.</returns>
        public abstract Uri GetResourceUri();
    }
}