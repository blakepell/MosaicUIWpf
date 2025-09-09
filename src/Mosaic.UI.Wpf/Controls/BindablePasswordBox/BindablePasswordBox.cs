/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using System.Runtime.CompilerServices;
using System.Windows.Data;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a password input control that supports data binding for the password value.
    /// </summary>
    public class BindablePasswordBox : ContentControl
    {
        /// <summary>
        /// Represents a private field that holds a reference to a wrapped <see cref="PasswordBox"/> control.
        /// </summary>
        private PasswordBox? _passwordBox;

        /// <summary>
        /// Indicates whether a password change operation is currently in progress.
        /// </summary>
        private bool _isPasswordChanging;

        /// <summary>
        /// Identifies the <see cref="Password"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PasswordProperty = DependencyProperty.Register(
            nameof(Password), typeof(string), typeof(BindablePasswordBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                PasswordPropertyChanged, null, false, UpdateSourceTrigger.PropertyChanged));

        /// <summary>
        /// Gets or sets the password associated with the object.
        /// </summary>
        public string Password
        {
            get => (string)GetValue(PasswordProperty);
            set => SetValue(PasswordProperty, value);
        }

        /// <summary>
        /// Handles changes to the Password dependency property.
        /// </summary>
        /// <remarks>This method ensures that the <see cref="BindablePasswordBox"/> updates its internal
        /// password state when the Password property value changes.</remarks>
        /// <param name="d">The dependency object on which the property change occurred. This must be a <see
        /// cref="BindablePasswordBox"/>.</param>
        /// <param name="e">The event data for the property change, including the old and new values.</param>
        private static void PasswordPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BindablePasswordBox passwordBox)
            {
                passwordBox.UpdatePassword();
            }
        }

        /// <summary>
        /// Initializes static metadata for the <see cref="BindablePasswordBox"/> class.
        /// </summary>
        static BindablePasswordBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(BindablePasswordBox), new FrameworkPropertyMetadata(typeof(BindablePasswordBox)));
            BorderThicknessProperty.OverrideMetadata(typeof(BindablePasswordBox), new FrameworkPropertyMetadata(new Thickness(1)));

            // Keep a safe fallback default brush in metadata in case the theme resource is missing for whatever reason.
            BorderBrushProperty.OverrideMetadata(typeof(BindablePasswordBox), new FrameworkPropertyMetadata(Brushes.DarkGray));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BindablePasswordBox"/> class.
        /// </summary>
        /// <remarks>This constructor sets up the <see cref="BindablePasswordBox"/> control and subscribes
        /// to the <see cref="FrameworkElement.Unloaded"/> event to handle cleanup when the control is
        /// unloaded.</remarks>
        public BindablePasswordBox()
        {
            // Set our resource for the default binding.  The caller is welcome to override it.
            SetResourceReference(BorderBrushProperty, MosaicTheme.ControlBorderBrushKey);

            // Set default binding for Password property to allow two-way binding in XAML without extra code.
            Unloaded += BindablePasswordBox_Unloaded;
        }

        /// <summary>
        /// Called when the template is applied to the control.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // Detach handler from previous template child to avoid duplicates/leaks
            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
            }

            _passwordBox = GetTemplateChild("PART_PasswordBox") as PasswordBox;

            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged += PasswordBox_PasswordChanged;
                UpdatePassword();
            }
        }

        /// <summary>
        /// Handles the <see cref="FrameworkElement.Unloaded"/> event for the BindablePasswordBox.
        /// </summary>
        /// <remarks>This method detaches the event handler for the <see
        /// cref="PasswordBox.PasswordChanged"/> event  and releases the reference to the associated <see
        /// cref="PasswordBox"/> to prevent memory leaks.</remarks>
        /// <param name="sender">The source of the event, typically the <see cref="BindablePasswordBox"/>.</param>
        /// <param name="e">The event data associated with the <see cref="FrameworkElement.Unloaded"/> event.</param>
        private void BindablePasswordBox_Unloaded(object? sender, RoutedEventArgs e)
        {
            // Free resources (e.g. event handlers) to prevent handler memory leaks
            if (_passwordBox != null)
            {
                _passwordBox.PasswordChanged -= PasswordBox_PasswordChanged;
                _passwordBox = null;
            }
        }

        /// <summary>
        /// Handles the <see cref="PasswordBox.PasswordChanged"/> event to synchronize the password value.
        /// </summary>
        /// <remarks>This method updates the <see cref="Password"/> property to reflect the current value
        /// of the <see cref="PasswordBox.Password"/>. It ensures that the update process is tracked using the
        /// <c>_isPasswordChanging</c> flag to prevent recursive updates.</remarks>
        /// <param name="sender">The source of the event, typically the <see cref="PasswordBox"/> whose password was changed.</param>
        /// <param name="e">The event data associated with the <see cref="PasswordBox.PasswordChanged"/> event.</param>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (_passwordBox == null)
            {
                return;
            }

            _isPasswordChanging = true;
            Password = _passwordBox.Password;
            _isPasswordChanging = false;
        }

        /// <summary>
        /// Updates the password displayed in the password box if the password change process is not active.
        /// </summary>
        /// <remarks>This method ensures that the password box reflects the current password value unless
        /// a password change operation is in progress.</remarks>
        private void UpdatePassword()
        {
            if (!_isPasswordChanging && _passwordBox != null)
            {
                _passwordBox.Password = Password;
            }
        }
    }
}
