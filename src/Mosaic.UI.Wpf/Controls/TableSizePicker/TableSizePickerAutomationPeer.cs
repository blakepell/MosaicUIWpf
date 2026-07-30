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

using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes a <see cref="TableSizePicker"/> to UI Automation as a single element whose value describes the
    /// region currently previewed or committed, for example <c>4 rows by 6 columns</c>.
    /// </summary>
    internal sealed class TableSizePickerAutomationPeer : FrameworkElementAutomationPeer, IValueProvider
    {
        public TableSizePickerAutomationPeer(TableSizePicker owner) : base(owner)
        {
        }

        private TableSizePicker OwnerPicker => (TableSizePicker)this.Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(TableSizePicker);
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            return string.IsNullOrWhiteSpace(name) ? "Table size picker" : name;
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Value)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public bool IsReadOnly => true;

        /// <inheritdoc />
        public string Value
        {
            get
            {
                var picker = this.OwnerPicker;
                int rows = picker.PreviewRowCount > 0 ? picker.PreviewRowCount : picker.SelectedRowCount;
                int columns = picker.PreviewColumnCount > 0 ? picker.PreviewColumnCount : picker.SelectedColumnCount;

                if (rows <= 0 || columns <= 0)
                {
                    return "No table size selected";
                }

                return $"{rows} {(rows == 1 ? "row" : "rows")} by {columns} {(columns == 1 ? "column" : "columns")}";
            }
        }

        /// <summary>
        /// The picker's value is controlled by the user's pointer or keyboard, it cannot be set programmatically
        /// through the value pattern.
        /// </summary>
        /// <param name="value">Unused.</param>
        public void SetValue(string value)
        {
            throw new InvalidOperationException("The TableSizePicker value is read only.");
        }
    }
}
