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
    /// Exposes a <see cref="FileCard"/> to UI Automation as an invokable button whose name is the file it
    /// represents.
    /// </summary>
    internal sealed class FileCardAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileCardAutomationPeer"/> class.
        /// </summary>
        /// <param name="owner">The card this peer represents.</param>
        public FileCardAutomationPeer(FileCard owner) : base(owner)
        {
        }

        /// <summary>
        /// Gets the owning card.
        /// </summary>
        private FileCard OwnerFileCard => (FileCard)Owner;

        /// <inheritdoc />
        protected override string GetClassNameCore()
        {
            return nameof(FileCard);
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Button;
        }

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (!string.IsNullOrWhiteSpace(name))
            {
                return name;
            }

            // Fall back to the displayed file name, with the size appended so a screen reader announces the
            // same information a sighted user sees on the card.
            string fileName = OwnerFileCard.FileName;

            if (string.IsNullOrWhiteSpace(fileName))
            {
                return string.Empty;
            }

            string size = OwnerFileCard.FileSizeText;

            return string.IsNullOrWhiteSpace(size) ? fileName : $"{fileName}, {size}";
        }

        /// <inheritdoc />
        protected override string GetHelpTextCore()
        {
            return OwnerFileCard.FileExists
                ? OwnerFileCard.FilePath ?? string.Empty
                : $"File not found: {OwnerFileCard.FilePath}";
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Invoke)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        /// <summary>
        /// Clicks the card, raising its <see cref="FileCard.Click"/> event and executing its command.
        /// </summary>
        public void Invoke()
        {
            if (!OwnerFileCard.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerFileCard.RaiseClick();
        }
    }
}
