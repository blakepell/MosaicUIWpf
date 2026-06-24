using System;
using System.Windows;

namespace Mosaic.UI.Wpf.AvalonDock.Themes
{
	/// <summary>
	/// Represents the dictionary Theme.
	/// </summary>
	public abstract class DictionaryTheme : Theme
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DictionaryTheme"/> class.
		/// </summary>
		public DictionaryTheme()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DictionaryTheme"/> class.
		/// </summary>
		/// <param name="themeResourceDictionary">The theme Resource Dictionary.</param>
		public DictionaryTheme(ResourceDictionary themeResourceDictionary)
		{
			ThemeResourceDictionary = themeResourceDictionary;
		}

		/// <summary>
		/// Gets the theme Resource Dictionary.
		/// </summary>
		public ResourceDictionary ThemeResourceDictionary { get; private set; }

		/// <inheritdoc/>
		public override Uri GetResourceUri()
		{
			return null;
		}
	}
}