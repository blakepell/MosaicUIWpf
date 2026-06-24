using Mosaic.UI.Wpf.AvalonDock.Layout;

namespace Mosaic.UI.Wpf.AvalonDock.Interfaces
{
    /// <summary>
    /// Interface for layout elements that behave like a <see cref="LayoutAnchorablePane"/>
    /// or an equivalent pane container such as <see cref="LayoutAnchorablePaneGroup"/>.
    /// </summary>
    public interface ILayoutAnchorablePane : ILayoutPanelElement, ILayoutPane
    {
    }
}