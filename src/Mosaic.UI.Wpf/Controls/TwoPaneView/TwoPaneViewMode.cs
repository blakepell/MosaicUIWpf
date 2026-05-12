namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Defines constants that specify how panes are shown in a <see cref="TwoPaneView"/>.
    /// </summary>
    /// <seealso cref="TwoPaneView"/>
    /// <seealso cref="TwoPaneView.Mode"/>
    public enum TwoPaneViewMode
    {
        /// <summary>
        /// Only one pane is shown.
        /// </summary>
        SinglePane = 0,

        /// <summary>
        /// Panes are shown side-by-side.
        /// </summary>
        Wide = 1,

        /// <summary>
        /// Panes are shown top-bottom.
        /// </summary>
        Tall = 2,
    }
}
