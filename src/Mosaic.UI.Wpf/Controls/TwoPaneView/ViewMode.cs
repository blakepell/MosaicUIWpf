namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Specifies the available layout modes for displaying content panes within the user interface.
    /// </summary>
    /// <remarks>Use this enumeration to control how panes are arranged or shown in the UI. Each value
    /// represents a distinct arrangement, such as displaying only one pane or splitting the view horizontally or
    /// vertically. This enumeration is intended for internal use within the application.</remarks>
    internal enum ViewMode
    {
        /// <summary>
        /// Specifies that only the first pane is visible in the layout.
        /// </summary>
        Pane1Only,
        /// <summary>
        /// Specifies that only the second pane is visible in the layout.
        /// </summary>
        Pane2Only,
        /// <summary>
        /// Specifies a horizontal orientation with left and right directions.
        /// </summary>
        LeftRight,
        /// <summary>
        /// Specifies that content flows from right to left.
        /// </summary>
        RightLeft,
        /// <summary>
        /// Specifies that the layout or operation applies to both the top and bottom edges or positions.
        /// </summary>
        TopBottom,
        /// <summary>
        /// Specifies that layout or content flows from the bottom to the top.
        /// </summary>
        BottomTop,
        /// <summary>
        /// Specifies that no value is set or that no option is selected.
        /// </summary>
        None
    };
}
