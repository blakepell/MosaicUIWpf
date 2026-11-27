using Mosaic.UI.Wpf.AvalonDock.Interfaces;
using System;
using System.Windows.Markup;
using System.Xml.Serialization;

namespace Mosaic.UI.Wpf.AvalonDock.Layout
{
    /// <summary>
    /// Represents a layout anchor group.
    /// </summary>
    [ContentProperty(nameof(Children))]
    [Serializable]
    public class LayoutAnchorGroup : LayoutGroup<LayoutAnchorable>, ILayoutPreviousContainer, ILayoutPaneSerializable, ISerializableLayoutPane
    {
        /// <inheritdoc/>
        protected override bool GetVisibility() => Children.Count > 0;

        [field: NonSerialized]
        private ILayoutContainer? _previousContainer;

        /// <inheritdoc/>
        [XmlIgnore]
        ILayoutContainer? ILayoutPreviousContainer.PreviousContainer
        {
            get => _previousContainer;
            set
            {
                if (value == _previousContainer)
                {
                    return;
                }

                _previousContainer = value;
                RaisePropertyChanged(nameof(ILayoutPreviousContainer.PreviousContainer));
                if (_previousContainer is ILayoutPaneSerializable { Id: null } paneSerializable)
                {
                    paneSerializable.Id = Guid.NewGuid().ToString();
                }
            }
        }

        /// <inheritdoc/>
        string? ILayoutPreviousContainer.PreviousContainerId { get; set; }

        private string? _id;

        /// <inheritdoc/>
        string? ILayoutPaneSerializable.Id { get => _id; set => _id = value; }

        /// <inheritdoc/>
        string? ISerializableLayoutPane.Id { get => _id; set => _id = value; }

        /// <summary>
        /// Gets a value indicating whether this group is directly hosted in a floating window. An auto-hide
        /// group is never hosted directly in a floating window, so this is always <c>false</c>. It exists so
        /// template bindings to <c>Model.Parent.IsDirectlyHostedInFloatingWindow</c> resolve without binding
        /// errors when an anchorable is auto-hidden.
        /// </summary>
        [XmlIgnore]
        public bool IsDirectlyHostedInFloatingWindow => false;
    }
}
