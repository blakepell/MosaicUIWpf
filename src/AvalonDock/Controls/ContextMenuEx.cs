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
        /// <summary>
        /// Initializes static members of the <see cref="ContextMenuEx"/> class.
        /// </summary>
        static ContextMenuEx()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextMenuEx"/> class.
        /// </summary>
        public ContextMenuEx()
        {
        }

        /// <inheritdoc/>
        protected override DependencyObject GetContainerForItemOverride()
        {
            return new MenuItemEx();
        }

        /// <inheritdoc/>
        protected override void OnOpened(RoutedEventArgs e)
        {
            BindingOperations.GetBindingExpression(this, ItemsSourceProperty).UpdateTarget();

            base.OnOpened(e);
        }
    }
}