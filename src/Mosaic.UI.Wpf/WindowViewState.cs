/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace Mosaic.UI.Wpf
{
    /// <summary>
    /// Window state information for a WPF Window on a specific workstation.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public partial class WindowViewState : ObservableObject
    {
        /// <summary>
        /// The name of the workstation the Window state information is for.
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The name of the workstation the Window state information is for.")]
        [ObservableProperty]
        private string? _machineName = Environment.MachineName;

        /// <summary>
        /// The specific Window Type
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The type of Window this metadata is for.")]
        [ObservableProperty]
        private string? _windowType;

        /// <summary>
        /// The saved height of the main window.
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The saved height of the main window.")]
        [ObservableProperty]
        private double _height = 768.0;

        /// <summary>
        /// The saved width of the main window.
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The saved width of the main window.")]
        [ObservableProperty]
        private double _width = 1024.0;

        /// <summary>
        /// The saved left of the main window.
        /// </summary>
        [property: Description("The saved left position of the main window.")]
        [property: Browsable(true)]
        [ObservableProperty]
        private double _left;

        /// <summary>
        /// The saved top of the main window.
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The saved top position of the main window.")]
        [ObservableProperty]
        private double _top;

        /// <summary>
        /// The window state
        /// </summary>
        [property: Browsable(true)]
        [property: Description("The window state.")]
        [ObservableProperty]
        private WindowState _windowState = WindowState.Maximized;

        /// <summary>
        /// XML for the layout of the dock if one exists.
        /// </summary>
        [Browsable(true)]
        [Description("XML that defines the layout of the dock windows for client.")]
        public string? LayoutXml { get; set; }

        public override string ToString()
        {
            return $"{this.MachineName}";
        }
    }
}
