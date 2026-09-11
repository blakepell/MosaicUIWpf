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
    /// Exposes a <see cref="LineGraph"/> to UI Automation. The graph's <see cref="LineGraph.Title"/> is used
    /// as the automation name when one is set, and the help text summarises the plotted series so a screen
    /// reader can describe what the picture shows.
    /// </summary>
    internal sealed class LineGraphAutomationPeer : FrameworkElementAutomationPeer
    {
        public LineGraphAutomationPeer(LineGraph owner) : base(owner)
        {
        }

        private LineGraph OwnerGraph => (LineGraph)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(LineGraph);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Custom;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            return string.IsNullOrWhiteSpace(this.OwnerGraph.Title) ? "Line graph" : this.OwnerGraph.Title;
        }

        /// <inheritdoc />
        protected override string GetHelpTextCore()
        {
            var help = base.GetHelpTextCore();

            if (!string.IsNullOrWhiteSpace(help))
            {
                return help;
            }

            var series = this.OwnerGraph.Series;

            if (series is null || series.Count == 0)
            {
                return "Line graph with no data.";
            }

            var captions = string.Join(", ", series.Select(s => string.IsNullOrWhiteSpace(s.Caption) ? "unnamed" : s.Caption));
            return $"Line graph with {series.Count} series: {captions}.";
        }
    }
}
