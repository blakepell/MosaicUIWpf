namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Defines constants that specify how panes are shown in a <see cref="TwoPaneView"/> in tall mode.
    /// </summary>
    /// <seealso cref="TwoPaneView"/>
    /// <seealso cref="TwoPaneView.TallModeConfiguration"/>
    public enum TwoPaneViewTallModeConfiguration
    {
        /// <summary>
        /// Only the pane that has priority is shown, the other pane is hidden.
        /// </summary>
        SinglePane = 0,

        /// <summary>
        /// The pane that has priority is shown on top, the other pane is shown on the bottom.
        /// </summary>
        TopBottom = 1,

        /// <summary>
        /// The pane that has priority is shown on the bottom, the other pane is shown on top.
        /// </summary>
        BottomTop = 2,
    }
}
