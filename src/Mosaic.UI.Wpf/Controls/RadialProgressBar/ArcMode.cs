namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Specifies how an <see cref="Arc"/> renders its progress.
    /// </summary>
    public enum ArcMode
    {
        /// <summary>
        /// Renders the progress as a filled arc. This is the default mode.
        /// </summary>
        Fill,

        /// <summary>
        /// Renders the progress as a ring of discrete shapes.
        /// </summary>
        Shape,

        /// <summary>
        /// Renders the progress as a filled pie slice.
        /// </summary>
        Pie
    }
}
