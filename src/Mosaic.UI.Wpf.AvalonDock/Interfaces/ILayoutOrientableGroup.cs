using System.Windows.Controls;

namespace Mosaic.UI.Wpf.AvalonDock.Interfaces
{
    /// <summary>
    /// Interface for layout orientable group.
    /// </summary>
    public interface ILayoutOrientableGroup : ILayoutGroup
    {
        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        Orientation Orientation { get; set; }
    }
}