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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes a <see cref="SqliteQueryControl"/> to UI Automation as a pane whose children are the
    /// database explorer, the SQL editor and the results grid.
    /// </summary>
    public class SqliteQueryControlAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteQueryControlAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="SqliteQueryControl"/> this peer represents.</param>
        public SqliteQueryControlAutomationPeer(SqliteQueryControl owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Pane;
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(SqliteQueryControl);
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return this.Owner is SqliteQueryControl { Schema.DatabaseName: { Length: > 0 } databaseName }
                ? $"SQLite query, {databaseName}"
                : "SQLite query";
        }
    }
}
