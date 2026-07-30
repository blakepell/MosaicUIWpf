/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A single selectable cell rendered by a <see cref="TableSizePicker"/>. Instances are created and owned by
    /// the picker and are only mutated by it, consumers bind to the state flags from an item template.
    /// </summary>
    public sealed class TableSizePickerCell : ObservableObject
    {
        private bool _isPreviewSelected;
        private bool _isCommittedSelected;
        private bool _isAnchor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TableSizePickerCell"/> class.
        /// </summary>
        /// <param name="row">The one based row of the cell.</param>
        /// <param name="column">The one based column of the cell.</param>
        internal TableSizePickerCell(int row, int column)
        {
            this.Row = row;
            this.Column = column;
        }

        /// <summary>
        /// The one based row this cell occupies.
        /// </summary>
        public int Row { get; }

        /// <summary>
        /// The one based column this cell occupies.
        /// </summary>
        public int Column { get; }

        /// <summary>
        /// Whether the cell falls inside the region currently being previewed via the mouse or keyboard.
        /// </summary>
        public bool IsPreviewSelected
        {
            get => _isPreviewSelected;
            internal set => this.SetProperty(ref _isPreviewSelected, value);
        }

        /// <summary>
        /// Whether the cell falls inside the region the user has committed by clicking. This is suppressed while
        /// a preview is active so that only one region is ever highlighted.
        /// </summary>
        public bool IsCommittedSelected
        {
            get => _isCommittedSelected;
            internal set => this.SetProperty(ref _isCommittedSelected, value);
        }

        /// <summary>
        /// Whether this cell is the lower right corner of the active region, that is the cell under the mouse or
        /// the cell the keyboard cursor sits on. Used to render a focus/hover affordance that does not rely on
        /// color alone.
        /// </summary>
        public bool IsAnchor
        {
            get => _isAnchor;
            internal set => this.SetProperty(ref _isAnchor, value);
        }
    }
}
