using Mosaic.UI.Wpf.AvalonDock.Interfaces;
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.AvalonDock.Layout
{
    /// <summary>
    /// Represents a layout document pane group.
    /// </summary>
    [ContentProperty(nameof(Children))]
    [Serializable]
    public class LayoutDocumentPaneGroup : LayoutPositionableGroup<ILayoutDocumentPane>, ILayoutDocumentPane, ILayoutOrientableGroup
    {
        private Orientation _orientation;

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutDocumentPaneGroup"/> class.
        /// </summary>
        public LayoutDocumentPaneGroup()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="LayoutDocumentPaneGroup"/> class.
        /// </summary>
        /// <param name="documentPane">The document pane.</param>
        public LayoutDocumentPaneGroup(LayoutDocumentPane documentPane)
        {
            Children.Add(documentPane);
        }

        /// <summary>
        /// Gets or sets the orientation.
        /// </summary>
        public Orientation Orientation
        {
            get => _orientation;
            set
            {
                if (value == _orientation)
                {
                    return;
                }

                RaisePropertyChanging(nameof(Orientation));
                _orientation = value;
                RaisePropertyChanged(nameof(Orientation));
            }
        }

        /// <inheritdoc/>
        protected override bool GetVisibility() => true;

#if TRACE
        /// <inheritdoc />
        public override void ConsoleDump(int tab)
        {
            Trace.Write(new string(' ', tab * 4));
            Trace.WriteLine($"DocumentPaneGroup({Orientation})");

            foreach (LayoutElement child in Children)
            {
                child.ConsoleDump(tab + 1);
            }
        }
#endif

    }
}