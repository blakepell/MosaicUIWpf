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

using System.Windows.Automation;
using System.Windows.Automation.Peers;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes a <see cref="Webcam"/> to UI Automation as an image whose item status reports the camera state.
    /// </summary>
    public class WebcamAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WebcamAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The <see cref="Webcam"/> this peer represents.</param>
        public WebcamAutomationPeer(Webcam owner) : base(owner)
        {
        }

        private Webcam Webcam => (Webcam)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(Controls.Webcam);
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return Webcam.Device is { } device ? $"Webcam preview: {device.Name}" : "Webcam preview";
        }

        /// <inheritdoc />
        protected override string GetItemStatusCore()
        {
            return Webcam.State.ToString();
        }

        /// <summary>
        /// Notifies automation clients that the camera state (exposed as item status) changed.
        /// </summary>
        internal void RaiseStateChanged(WebcamState oldState, WebcamState newState)
        {
            RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty, oldState.ToString(), newState.ToString());
        }
    }
}
