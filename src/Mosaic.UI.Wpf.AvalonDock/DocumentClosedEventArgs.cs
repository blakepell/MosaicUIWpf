using System;
using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock
{
	/// <summary>
	/// Provides data for the document Closed event.
	/// </summary>
	public class DocumentClosedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocumentClosedEventArgs"/> class.
		/// </summary>
		/// <param name="document">The document.</param>
		public DocumentClosedEventArgs(LayoutDocument document)
		{
			Document = document;
		}

		/// <summary>
		/// Gets the document.
		/// </summary>
		public LayoutDocument Document { get; private set; }
	}
}