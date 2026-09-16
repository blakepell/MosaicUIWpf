/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Common;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A button that renders another element to an image and either copies it to the clipboard or saves
    /// it to a file, as selected by <see cref="Mode"/>.  The element is identified by <see cref="TargetName"/>
    /// (resolved through the button's name scope) or bound directly via <see cref="Target"/>.  If the
    /// target scrolls, its entire scroll extent is captured.
    /// </summary>
    /// <remarks>
    /// <code><![CDATA[
    /// <DataGrid x:Name="Grid" ... />
    /// <mosaic:VisualCaptureButton TargetName="Grid" Mode="CopyToClipboard" />
    /// <mosaic:VisualCaptureButton Target="{Binding ElementName=Grid}" Mode="SaveToFile" Content="Export PNG" />
    /// ]]></code>
    /// </remarks>
    [DefaultEvent(nameof(Captured))]
    [DefaultProperty(nameof(TargetName))]
    public class VisualCaptureButton : AccentButton
    {
        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target), typeof(UIElement), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="TargetName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register(
            nameof(TargetName), typeof(string), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="Mode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
            nameof(Mode), typeof(VisualCaptureMode), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(VisualCaptureMode.CopyToClipboard));

        /// <summary>
        /// Identifies the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath), typeof(string), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="DefaultFileName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultFileNameProperty = DependencyProperty.Register(
            nameof(DefaultFileName), typeof(string), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CaptureBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaptureBackgroundProperty = DependencyProperty.Register(
            nameof(CaptureBackground), typeof(Brush), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CaptureScrollExtent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaptureScrollExtentProperty = DependencyProperty.Register(
            nameof(CaptureScrollExtent), typeof(bool), typeof(VisualCaptureButton),
            new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="Captured"/> routed event.
        /// </summary>
        public static readonly RoutedEvent CapturedEvent = EventManager.RegisterRoutedEvent(
            nameof(Captured), RoutingStrategy.Bubble, typeof(EventHandler<VisualCapturedEventArgs>), typeof(VisualCaptureButton));

        /// <summary>
        /// Identifies the <see cref="CaptureFailed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent CaptureFailedEvent = EventManager.RegisterRoutedEvent(
            nameof(CaptureFailed), RoutingStrategy.Bubble, typeof(EventHandler<VisualCapturedEventArgs>), typeof(VisualCaptureButton));

        /// <summary>
        /// Initializes a new instance of the <see cref="VisualCaptureButton"/> class.
        /// </summary>
        public VisualCaptureButton()
        {
            this.DefaultStyleKey = typeof(VisualCaptureButton);
        }

        /// <summary>
        /// Gets or sets the element to capture.  Takes precedence over <see cref="TargetName"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("The element to capture. Takes precedence over TargetName.")]
        public UIElement? Target
        {
            get => (UIElement?)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets the x:Name of the element to capture.  The name is resolved through the button's
        /// name scope and, failing that, the name scopes of its logical ancestors.
        /// </summary>
        [Category("Behavior")]
        [Description("The x:Name of the element to capture.")]
        public string? TargetName
        {
            get => (string?)GetValue(TargetNameProperty);
            set => SetValue(TargetNameProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the image is copied to the clipboard or saved to a file when clicked.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether the image is copied to the clipboard or saved to a file when clicked.")]
        public VisualCaptureMode Mode
        {
            get => (VisualCaptureMode)GetValue(ModeProperty);
            set => SetValue(ModeProperty, value);
        }

        /// <summary>
        /// Gets or sets the file the image is written to when <see cref="Mode"/> is
        /// <see cref="VisualCaptureMode.SaveToFile"/>.  When empty the user is prompted with a save dialog.
        /// </summary>
        [Category("Behavior")]
        [Description("The file to save to. When empty the user is prompted with a save dialog.")]
        public string? FilePath
        {
            get => (string?)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the file name pre-populated in the save dialog.
        /// </summary>
        [Category("Behavior")]
        [Description("The file name pre-populated in the save dialog.")]
        public string? DefaultFileName
        {
            get => (string?)GetValue(DefaultFileNameProperty);
            set => SetValue(DefaultFileNameProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush painted behind the captured element.  Leave <see langword="null"/> to keep
        /// unpainted areas transparent.
        /// </summary>
        [Category("Appearance")]
        [Description("A brush painted behind the captured element. Null keeps unpainted areas transparent.")]
        public Brush? CaptureBackground
        {
            get => (Brush?)GetValue(CaptureBackgroundProperty);
            set => SetValue(CaptureBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets whether a scrolling target is expanded so its full scroll extent is captured.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether a scrolling target is expanded so its full scroll extent is captured.")]
        public bool CaptureScrollExtent
        {
            get => (bool)GetValue(CaptureScrollExtentProperty);
            set => SetValue(CaptureScrollExtentProperty, value);
        }

        /// <summary>
        /// Occurs after the image has been copied or saved.  Not raised when the user cancels the save dialog.
        /// </summary>
        public event EventHandler<VisualCapturedEventArgs> Captured
        {
            add => AddHandler(CapturedEvent, value);
            remove => RemoveHandler(CapturedEvent, value);
        }

        /// <summary>
        /// Occurs when the capture could not be completed, for example because the target could not be
        /// found or the clipboard was unavailable.
        /// </summary>
        public event EventHandler<VisualCapturedEventArgs> CaptureFailed
        {
            add => AddHandler(CaptureFailedEvent, value);
            remove => RemoveHandler(CaptureFailedEvent, value);
        }

        /// <summary>
        /// Resolves the element that will be captured from <see cref="Target"/> or <see cref="TargetName"/>.
        /// </summary>
        /// <returns>The element, or <see langword="null"/> if none could be found.</returns>
        public UIElement? ResolveTarget()
        {
            if (this.Target != null)
            {
                return this.Target;
            }

            return string.IsNullOrWhiteSpace(this.TargetName) ? null : FindElementByName(this, this.TargetName);
        }

        /// <summary>
        /// Performs the capture described by <see cref="Mode"/> immediately, exactly as a click would.
        /// </summary>
        /// <returns><see langword="true"/> when the capture completed.</returns>
        public bool Capture()
        {
            var target = this.ResolveTarget();

            if (target == null)
            {
                var missing = new InvalidOperationException(string.IsNullOrWhiteSpace(this.TargetName)
                    ? "No Target or TargetName has been set on the VisualCaptureButton."
                    : $"No element named '{this.TargetName}' could be found from the VisualCaptureButton.");

                RaiseEvent(new VisualCapturedEventArgs(CaptureFailedEvent, this, this.Mode, null, null, missing));
                return false;
            }

            var options = new VisualCaptureOptions
            {
                Background = this.CaptureBackground,
                CaptureScrollExtent = this.CaptureScrollExtent,
                DefaultFileName = this.DefaultFileName
            };

            try
            {
                string? path = null;

                if (this.Mode == VisualCaptureMode.SaveToFile)
                {
                    if (!string.IsNullOrWhiteSpace(this.FilePath))
                    {
                        VisualCapture.SaveToFile(target, this.FilePath, options);
                        path = this.FilePath;
                    }
                    else
                    {
                        path = VisualCapture.SaveToFile(target, options);

                        if (path == null)
                        {
                            // The user cancelled the dialog; neither success nor failure.
                            return false;
                        }
                    }
                }
                else
                {
                    VisualCapture.CopyToClipboard(target, options);
                }

                RaiseEvent(new VisualCapturedEventArgs(CapturedEvent, this, this.Mode, target, path, null));
                return true;
            }
            catch (Exception ex)
            {
                RaiseEvent(new VisualCapturedEventArgs(CaptureFailedEvent, this, this.Mode, target, null, ex));
                return false;
            }
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            this.Capture();
            base.OnClick();
        }

        /// <summary>
        /// Looks a name up in the name scope of the element and then each logical ancestor, which lets the
        /// button find siblings declared in the same XAML file as well as elements in an enclosing
        /// UserControl, Window, or template.
        /// </summary>
        internal static UIElement? FindElementByName(FrameworkElement start, string name)
        {
            DependencyObject? current = start;

            while (current != null)
            {
                if (current is FrameworkElement fe)
                {
                    if (fe.FindName(name) is UIElement found)
                    {
                        return found;
                    }

                    if (fe.TemplatedParent is FrameworkElement templated && templated.FindName(name) is UIElement inTemplate)
                    {
                        return inTemplate;
                    }
                }
                else if (current is FrameworkContentElement fce && fce.FindName(name) is UIElement foundContent)
                {
                    return foundContent;
                }

                current = LogicalTreeHelper.GetParent(current)
                          ?? (current is Visual or System.Windows.Media.Media3D.Visual3D ? VisualTreeHelper.GetParent(current) : null);
            }

            return null;
        }
    }
}
