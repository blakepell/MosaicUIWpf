using System;
using System.IO;
using System.Reflection;

namespace AvalonDock.Themes
{
	/// <summary>
	/// Blue theme loaded from an embedded .vstheme resource file.
	/// </summary>
	public class BlueTheme : VsTheme
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="BlueTheme"/> class.
		/// </summary>
		public BlueTheme()
			: base(LoadEmbeddedResource("AvalonDock.Themes.VisualStudio.Resources.blue.vstheme"))
		{
		}

		private static Stream LoadEmbeddedResource(string name)
		{
			return Assembly.GetExecutingAssembly().GetManifestResourceStream(name)
				?? throw new InvalidOperationException($"Embedded theme resource '{name}' was not found.");
		}
	}
}
