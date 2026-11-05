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
    /// Exposes <see cref="ImageViewer"/> to UI Automation as an image that supports rotation through
    /// <see cref="ITransformProvider"/>.
    /// </summary>
    internal sealed class ImageViewerAutomationPeer : FrameworkElementAutomationPeer, ITransformProvider
    {
        public ImageViewerAutomationPeer(ImageViewer owner) : base(owner)
        {
        }

        private ImageViewer OwnerViewer => (ImageViewer)Owner;

        protected override string GetClassNameCore()
        {
            return nameof(ImageViewer);
        }

        protected override AutomationControlType GetAutomationControlTypeCore()
        {
            return AutomationControlType.Image;
        }

        protected override string GetNameCore()
        {
            string name = base.GetNameCore();

            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            string? path = OwnerViewer.ImagePath;
            return string.IsNullOrWhiteSpace(path) ? string.Empty : System.IO.Path.GetFileName(path);
        }

        protected override string GetHelpTextCore()
        {
            string help = base.GetHelpTextCore();

            if (!string.IsNullOrEmpty(help) || !OwnerViewer.HasImage)
            {
                return help;
            }

            return $"{OwnerViewer.ResolutionText}, {OwnerViewer.Zoom:P0}";
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            if (patternInterface == PatternInterface.Transform)
            {
                return this;
            }

            return base.GetPattern(patternInterface);
        }

        public bool CanMove => false;

        public bool CanResize => false;

        public bool CanRotate => OwnerViewer.HasImage;

        public void Move(double x, double y)
        {
            throw new InvalidOperationException("The image viewer cannot be moved.");
        }

        public void Resize(double width, double height)
        {
            throw new InvalidOperationException("The image viewer cannot be resized.");
        }

        public void Rotate(double degrees)
        {
            if (!OwnerViewer.IsEnabled)
            {
                throw new ElementNotEnabledException();
            }

            OwnerViewer.SetCurrentValue(ImageViewer.RotationProperty, OwnerViewer.Rotation + degrees);
        }
    }
}
