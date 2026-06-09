using System;
using System.IO;
using System.Reflection;

namespace AvalonDock.Themes
{
	/// <summary>
	/// Light theme loaded from an embedded .vstheme resource file.
	/// </summary>
	public class LightTheme : VsTheme
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LightTheme"/> class.
		/// </summary>
		public LightTheme()
			: base(LoadEmbeddedResource("AvalonDock.Themes.VisualStudio.Resources.light.vstheme"))
		{
		}

		private static Stream LoadEmbeddedResource(string name)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
				?? throw new InvalidOperationException($"Embedded theme resource '{name}' was not found.");
		}
	}
}
