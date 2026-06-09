using System;
using System.IO;
using System.Reflection;

namespace AvalonDock.Themes
{
	/// <summary>
	/// Dark theme loaded from an embedded .vstheme resource file.
	/// </summary>
	public class DarkTheme : VsTheme
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DarkTheme"/> class.
		/// </summary>
		public DarkTheme()
			: base(LoadEmbeddedResource("AvalonDock.Themes.VisualStudio.Resources.dark.vstheme"))
		{
		}

		private static Stream LoadEmbeddedResource(string name)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
				?? throw new InvalidOperationException($"Embedded theme resource '{name}' was not found.");
		}
	}
}
