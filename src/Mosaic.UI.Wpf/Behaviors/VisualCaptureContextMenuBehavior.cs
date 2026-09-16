/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Microsoft.Xaml.Behaviors;
using Mosaic.UI.Wpf.Common;
using Mosaic.UI.Wpf.Controls;

namespace Mosaic.UI.Wpf.Behaviors
{
    /// <summary>
    /// Adds "Copy as Image" and "Save as Image..." items to the context menu of the element the behavior
    /// is attached to.  When the element has no context menu one is created; when it already has one the
    /// items are appended after a separator.  By default the attached element itself is captured, but a
    /// different element can be chosen with <see cref="Target"/> or <see cref="TargetName"/>.  Scrolling
    /// elements are captured at their full scroll extent.
    /// </summary>
    /// <remarks>
    /// <code><![CDATA[
    /// <DataGrid x:Name="Grid">
    ///     <i:Interaction.Behaviors>
    ///         <mosaic:VisualCaptureContextMenuBehavior />
    ///     </i:Interaction.Behaviors>
    /// </DataGrid>
    /// ]]></code>
    /// Controls such as <see cref="TextBox"/> show a built-in editing menu when their ContextMenu is null.
    /// Attaching this behavior to such a control replaces that built-in menu; declare an explicit
    /// ContextMenu with the editing commands if both are needed.
    /// </remarks>
    public class VisualCaptureContextMenuBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target), typeof(UIElement), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="TargetName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetNameProperty = DependencyProperty.Register(
            nameof(TargetName), typeof(string), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CopyHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CopyHeaderProperty = DependencyProperty.Register(
            nameof(CopyHeader), typeof(object), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata("Copy as Image", OnHeaderChanged));

        /// <summary>
        /// Identifies the <see cref="SaveHeader"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SaveHeaderProperty = DependencyProperty.Register(
            nameof(SaveHeader), typeof(object), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata("Save as Image...", OnHeaderChanged));

        /// <summary>
        /// Identifies the <see cref="ShowCopy"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCopyProperty = DependencyProperty.Register(
            nameof(ShowCopy), typeof(bool), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(true, OnVisibilityChanged));

        /// <summary>
        /// Identifies the <see cref="ShowSave"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowSaveProperty = DependencyProperty.Register(
            nameof(ShowSave), typeof(bool), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(true, OnVisibilityChanged));

        /// <summary>
        /// Identifies the <see cref="InsertSeparator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InsertSeparatorProperty = DependencyProperty.Register(
            nameof(InsertSeparator), typeof(bool), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(true, OnVisibilityChanged));

        /// <summary>
        /// Identifies the <see cref="FilePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilePathProperty = DependencyProperty.Register(
            nameof(FilePath), typeof(string), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="DefaultFileName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DefaultFileNameProperty = DependencyProperty.Register(
            nameof(DefaultFileName), typeof(string), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CaptureBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaptureBackgroundProperty = DependencyProperty.Register(
            nameof(CaptureBackground), typeof(Brush), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="CaptureScrollExtent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CaptureScrollExtentProperty = DependencyProperty.Register(
            nameof(CaptureScrollExtent), typeof(bool), typeof(VisualCaptureContextMenuBehavior),
            new PropertyMetadata(true));

        private ContextMenu? _menu;
        private bool _ownsMenu;
        private Separator? _separator;
        private MenuItem? _copyItem;
        private MenuItem? _saveItem;

        /// <summary>
        /// Gets or sets the element to capture.  Defaults to the element the behavior is attached to and
        /// takes precedence over <see cref="TargetName"/>.
        /// </summary>
        public UIElement? Target
        {
            get => (UIElement?)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Gets or sets the x:Name of the element to capture, resolved from the attached element's name scope.
        /// </summary>
        public string? TargetName
        {
            get => (string?)GetValue(TargetNameProperty);
            set => SetValue(TargetNameProperty, value);
        }

        /// <summary>
        /// Gets or sets the header of the copy menu item.
        /// </summary>
        public object CopyHeader
        {
            get => GetValue(CopyHeaderProperty);
            set => SetValue(CopyHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets the header of the save menu item.
        /// </summary>
        public object SaveHeader
        {
            get => GetValue(SaveHeaderProperty);
            set => SetValue(SaveHeaderProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the copy item is shown.
        /// </summary>
        public bool ShowCopy
        {
            get => (bool)GetValue(ShowCopyProperty);
            set => SetValue(ShowCopyProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the save item is shown.
        /// </summary>
        public bool ShowSave
        {
            get => (bool)GetValue(ShowSaveProperty);
            set => SetValue(ShowSaveProperty, value);
        }

        /// <summary>
        /// Gets or sets whether a separator is inserted before the capture items when they are appended to
        /// an existing menu that already has items.
        /// </summary>
        public bool InsertSeparator
        {
            get => (bool)GetValue(InsertSeparatorProperty);
            set => SetValue(InsertSeparatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the file the image is saved to.  When empty the user is prompted with a save dialog.
        /// </summary>
        public string? FilePath
        {
            get => (string?)GetValue(FilePathProperty);
            set => SetValue(FilePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the file name pre-populated in the save dialog.
        /// </summary>
        public string? DefaultFileName
        {
            get => (string?)GetValue(DefaultFileNameProperty);
            set => SetValue(DefaultFileNameProperty, value);
        }

        /// <summary>
        /// Gets or sets a brush painted behind the captured element.  Leave <see langword="null"/> to keep
        /// unpainted areas transparent.
        /// </summary>
        public Brush? CaptureBackground
        {
            get => (Brush?)GetValue(CaptureBackgroundProperty);
            set => SetValue(CaptureBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets whether a scrolling target is expanded so its full scroll extent is captured.
        /// </summary>
        public bool CaptureScrollExtent
        {
            get => (bool)GetValue(CaptureScrollExtentProperty);
            set => SetValue(CaptureScrollExtentProperty, value);
        }

        /// <summary>
        /// Occurs after the image has been copied or saved.
        /// </summary>
        public event EventHandler<VisualCapturedEventArgs>? Captured;

        /// <summary>
        /// Occurs when the capture could not be completed.
        /// </summary>
        public event EventHandler<VisualCapturedEventArgs>? CaptureFailed;

        /// <inheritdoc />
        protected override void OnAttached()
        {
            base.OnAttached();

            _copyItem = new MenuItem { Header = this.CopyHeader };
            _copyItem.Click += OnCopyClick;

            _saveItem = new MenuItem { Header = this.SaveHeader };
            _saveItem.Click += OnSaveClick;

            _separator = new Separator();

            this.AssociatedObject.ContextMenuOpening += OnContextMenuOpening;
            this.AttachToMenu();
        }

        /// <inheritdoc />
        protected override void OnDetaching()
        {
            this.AssociatedObject.ContextMenuOpening -= OnContextMenuOpening;
            this.DetachFromMenu();

            if (_copyItem != null)
            {
                _copyItem.Click -= OnCopyClick;
            }

            if (_saveItem != null)
            {
                _saveItem.Click -= OnSaveClick;
            }

            _copyItem = null;
            _saveItem = null;
            _separator = null;

            base.OnDetaching();
        }

        /// <summary>
        /// Resolves the element that will be captured.
        /// </summary>
        /// <returns>The target, falling back to the attached element.</returns>
        public UIElement? ResolveTarget()
        {
            if (this.Target != null)
            {
                return this.Target;
            }

            if (!string.IsNullOrWhiteSpace(this.TargetName) && this.AssociatedObject != null)
            {
                return VisualCaptureButton.FindElementByName(this.AssociatedObject, this.TargetName);
            }

            return this.AssociatedObject;
        }

        /// <summary>
        /// Performs a capture of the resolved target.
        /// </summary>
        /// <param name="mode">Whether to copy or save.</param>
        /// <returns><see langword="true"/> when the capture completed.</returns>
        public bool Capture(VisualCaptureMode mode)
        {
            var source = (object?)this.AssociatedObject ?? this;
            var target = this.ResolveTarget();

            if (target == null)
            {
                var missing = new InvalidOperationException($"No element named '{this.TargetName}' could be found from the attached element.");
                this.CaptureFailed?.Invoke(this, new VisualCapturedEventArgs(VisualCaptureButton.CaptureFailedEvent, source, mode, null, null, missing));
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

                if (mode == VisualCaptureMode.SaveToFile)
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
                            return false;
                        }
                    }
                }
                else
                {
                    VisualCapture.CopyToClipboard(target, options);
                }

                this.Captured?.Invoke(this, new VisualCapturedEventArgs(VisualCaptureButton.CapturedEvent, source, mode, target, path, null));
                return true;
            }
            catch (Exception ex)
            {
                this.CaptureFailed?.Invoke(this, new VisualCapturedEventArgs(VisualCaptureButton.CaptureFailedEvent, source, mode, target, null, ex));
                return false;
            }
        }

        /// <summary>
        /// The context menu may be assigned after the behavior attaches (for example by a style), so the
        /// menu is re-checked each time it is about to open.
        /// </summary>
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!ReferenceEquals(_menu, this.AssociatedObject.ContextMenu))
            {
                this.DetachFromMenu();
                this.AttachToMenu();
            }
        }

        /// <summary>
        /// Adds the capture items to the attached element's context menu, creating the menu when needed.
        /// </summary>
        private void AttachToMenu()
        {
            if (_copyItem == null || _saveItem == null || _separator == null)
            {
                return;
            }

            var menu = this.AssociatedObject.ContextMenu;

            if (menu == null)
            {
                menu = new ContextMenu();
                this.AssociatedObject.ContextMenu = menu;
                _ownsMenu = true;
            }
            else
            {
                _ownsMenu = false;
            }

            _menu = menu;

            menu.Items.Add(_separator);
            menu.Items.Add(_copyItem);
            menu.Items.Add(_saveItem);

            this.UpdateItemVisibility();
        }

        /// <summary>
        /// Removes the capture items from the menu, and the menu itself if the behavior created it.
        /// </summary>
        private void DetachFromMenu()
        {
            if (_menu == null)
            {
                return;
            }

            _menu.Items.Remove(_separator);
            _menu.Items.Remove(_copyItem);
            _menu.Items.Remove(_saveItem);

            if (_ownsMenu && ReferenceEquals(this.AssociatedObject?.ContextMenu, _menu))
            {
                this.AssociatedObject.ContextMenu = null;
            }

            _menu = null;
            _ownsMenu = false;
        }

        /// <summary>
        /// Applies <see cref="ShowCopy"/>, <see cref="ShowSave"/>, and <see cref="InsertSeparator"/> to the
        /// items.  The separator is only shown when there are other items above it.
        /// </summary>
        private void UpdateItemVisibility()
        {
            if (_menu == null || _copyItem == null || _saveItem == null || _separator == null)
            {
                return;
            }

            _copyItem.Visibility = this.ShowCopy ? Visibility.Visible : Visibility.Collapsed;
            _saveItem.Visibility = this.ShowSave ? Visibility.Visible : Visibility.Collapsed;

            bool hasOtherItems = _menu.Items.IndexOf(_separator) > 0;
            bool anyShown = this.ShowCopy || this.ShowSave;

            _separator.Visibility = this.InsertSeparator && hasOtherItems && anyShown ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OnCopyClick(object sender, RoutedEventArgs e)
        {
            this.Capture(VisualCaptureMode.CopyToClipboard);
        }

        private void OnSaveClick(object sender, RoutedEventArgs e)
        {
            this.Capture(VisualCaptureMode.SaveToFile);
        }

        private static void OnHeaderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (VisualCaptureContextMenuBehavior)d;

            if (behavior._copyItem != null)
            {
                behavior._copyItem.Header = behavior.CopyHeader;
            }

            if (behavior._saveItem != null)
            {
                behavior._saveItem.Header = behavior.SaveHeader;
            }
        }

        private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((VisualCaptureContextMenuBehavior)d).UpdateItemVisibility();
        }
    }
}
