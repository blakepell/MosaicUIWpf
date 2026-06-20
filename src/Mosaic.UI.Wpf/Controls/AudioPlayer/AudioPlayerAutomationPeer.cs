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
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Exposes <see cref="AudioPlayer"/> to UI Automation, surfacing an <see cref="IInvokeProvider"/> that toggles
    /// playback between playing and stopped.
    /// </summary>
    internal sealed class AudioPlayerAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
    {
        public AudioPlayerAutomationPeer(AudioPlayer owner) : base(owner)
        {
        }

        private AudioPlayer OwnerPlayer => (AudioPlayer)Owner;

        protected override string GetClassNameCore()
        {
            return nameof(AudioPlayer);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Group;
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Toggles playback: stops if currently playing, otherwise starts playback.
        /// </summary>
        public void Invoke()
        {
            if (!OwnerPlayer.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            if (OwnerPlayer.IsPlaying)
            {
                OwnerPlayer.Stop();
            }
            else
            {
                OwnerPlayer.Play();
            }
        }
    }
}
