using System;

namespace Mosaic.UI.Wpf.AvalonDock.Themes
{
    /// <summary>
    /// AvalonDock theme that maps docking resources to Mosaic UI theme tokens.
    /// </summary>
    public class MosaicTheme : Theme
    {
        private static readonly Uri ResourceUri =
            new("/Mosaic.UI.Wpf.AvalonDock;component/Themes/Mosaic.xaml", UriKind.Relative);

        /// <inheritdoc/>
        public override Uri GetResourceUri()
        {
            return ResourceUri;
        }
    }
}
