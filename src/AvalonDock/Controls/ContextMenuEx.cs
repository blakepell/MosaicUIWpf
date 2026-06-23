using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace AvalonDock.Controls
{
    /// <summary>
    /// Represents the context Menu Ex.
    /// </summary>
    public class ContextMenuEx : ContextMenu
    {
        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MenuItemEx();
        }

        /// <inheritdoc/>
        protected override void OnOpened(RoutedEventArgs e)
        {
            BindingOperations.GetBindingExpression(this, ItemsSourceProperty)?.UpdateTarget();
            base.OnOpened(e);
        }
    }
}