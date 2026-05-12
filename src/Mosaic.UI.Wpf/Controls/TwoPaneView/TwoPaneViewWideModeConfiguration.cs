namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Defines constants that specify how panes are shown in a <see cref="TwoPaneView"/> in wide mode.
    /// </summary>
    /// <seealso cref="TwoPaneView"/>
    /// <seealso cref="TwoPaneView.WideModeConfiguration"/>
    public enum TwoPaneViewWideModeConfiguration
    {
        /// <summary>
        /// Only the pane that has priority is shown, the other pane is hidden.
        /// </summary>
        SinglePane = 0,

        /// <summary>
        /// The pane that has priority is shown on the left, the other pane is shown on the right.
        /// </summary>
        LeftRight = 1,

        /// <summary>
        /// The pane that has priority is shown on the right, the other pane is shown on the left.
        /// </summary>
        RightLeft = 2,
    }
}
