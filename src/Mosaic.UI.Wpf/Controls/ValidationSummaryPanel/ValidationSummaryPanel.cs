/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single validation error with its associated control information.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the property name associated with the error.
        /// </summary>
        public string PropertyName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the control that has the validation error.
        /// </summary>
        public FrameworkElement? SourceControl { get; set; }
    }

    /// <summary>
    /// A validation summary panel that displays all validation errors from child controls in a form.
    /// Supports both WPF's built-in validation and IDataErrorInfo/INotifyDataErrorInfo implementations.
    /// </summary>
    public class ValidationSummaryPanel : ItemsControl
    {
        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(
            nameof(Target),
            typeof(FrameworkElement),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(null, OnTargetChanged));

        /// <summary>
        /// Gets or sets the target container (e.g., a Grid or StackPanel) to scan for validation errors.
        /// If not set, the panel will attempt to find the nearest parent container.
        /// </summary>
        public FrameworkElement? Target
        {
            get => (FrameworkElement?)GetValue(TargetProperty);
            set => SetValue(TargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Title"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
            nameof(Title),
            typeof(string),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata("Validation Errors"));

        /// <summary>
        /// Gets or sets the title displayed at the top of the validation summary.
        /// </summary>
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconGeometry"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconGeometryProperty = DependencyProperty.Register(
            nameof(IconGeometry),
            typeof(Geometry),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the geometry for the icon displayed next to each error.
        /// </summary>
        public Geometry? IconGeometry
        {
            get => (Geometry?)GetValue(IconGeometryProperty);
            set => SetValue(IconGeometryProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IconBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IconBrushProperty = DependencyProperty.Register(
            nameof(IconBrush),
            typeof(Brush),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(Brushes.Red));

        /// <summary>
        /// Gets or sets the brush used to fill the error icon.
        /// </summary>
        public Brush IconBrush
        {
            get => (Brush)GetValue(IconBrushProperty);
            set => SetValue(IconBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderBackgroundProperty = DependencyProperty.Register(
            nameof(HeaderBackground),
            typeof(Brush),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(254, 226, 226))));

        /// <summary>
        /// Gets or sets the background brush for the header area.
        /// </summary>
        public Brush HeaderBackground
        {
            get => (Brush)GetValue(HeaderBackgroundProperty);
            set => SetValue(HeaderBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HasErrors"/> dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey HasErrorsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasErrors),
            typeof(bool),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="HasErrors"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasErrorsProperty = HasErrorsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether there are any validation errors.
        /// </summary>
        public bool HasErrors
        {
            get => (bool)GetValue(HasErrorsProperty);
            private set => SetValue(HasErrorsPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="ErrorCount"/> dependency property key.
        /// </summary>
        private static readonly DependencyPropertyKey ErrorCountPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ErrorCount),
            typeof(int),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="ErrorCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ErrorCountProperty = ErrorCountPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the total count of validation errors.
        /// </summary>
        public int ErrorCount
        {
            get => (int)GetValue(ErrorCountProperty);
            private set => SetValue(ErrorCountPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="AutoHideWhenValid"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoHideWhenValidProperty = DependencyProperty.Register(
            nameof(AutoHideWhenValid),
            typeof(bool),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(true, OnAutoHideWhenValidChanged));

        /// <summary>
        /// Gets or sets whether the panel should automatically hide when there are no errors.
        /// </summary>
        public bool AutoHideWhenValid
        {
            get => (bool)GetValue(AutoHideWhenValidProperty);
            set => SetValue(AutoHideWhenValidProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FocusControlOnClick"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FocusControlOnClickProperty = DependencyProperty.Register(
            nameof(FocusControlOnClick),
            typeof(bool),
            typeof(ValidationSummaryPanel),
            new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether clicking on an error should focus the associated control.
        /// </summary>
        public bool FocusControlOnClick
        {
            get => (bool)GetValue(FocusControlOnClickProperty);
            set => SetValue(FocusControlOnClickProperty, value);
        }

        /// <summary>
        /// Gets the collection of validation errors.
        /// </summary>
        public ObservableCollection<ValidationError> Errors { get; } = new();

        /// <summary>
        /// Command to focus an error's source control.
        /// </summary>
        public static readonly RoutedCommand FocusErrorCommand = new(nameof(FocusErrorCommand), typeof(ValidationSummaryPanel));

        /// <summary>
        /// Initializes the <see cref="ValidationSummaryPanel"/> class.
        /// </summary>
        static ValidationSummaryPanel()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ValidationSummaryPanel), 
                new FrameworkPropertyMetadata(typeof(ValidationSummaryPanel)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValidationSummaryPanel"/> class.
        /// </summary>
        public ValidationSummaryPanel()
        {
            ItemsSource = Errors;
            Errors.CollectionChanged += (_, _) => UpdateErrorState();
            Loaded += OnLoaded;

            // Register command binding
            CommandBindings.Add(new CommandBinding(FocusErrorCommand, OnFocusErrorExecuted));
        }

        /// <summary>
        /// Handles the FocusErrorCommand execution.
        /// </summary>
        private void OnFocusErrorExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ValidationError error)
            {
                FocusError(error);
            }
        }

        /// <summary>
        /// Handles the Loaded event.
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Target == null)
            {
                // Try to find the parent container
                Target = FindParentContainer();
            }

            Refresh();
        }

        /// <summary>
        /// Finds the nearest parent container to scan for validation errors.
        /// </summary>
        private FrameworkElement? FindParentContainer()
        {
            DependencyObject? current = this.Parent;
            
            while (current != null)
            {
                if (current is Panel or ContentControl or Window)
                {
                    return current as FrameworkElement;
                }
                current = VisualTreeHelper.GetParent(current);
            }

            return null;
        }

        /// <summary>
        /// Handles changes to the Target property.
        /// </summary>
        private static void OnTargetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidationSummaryPanel panel)
            {
                panel.DetachFromTarget(e.OldValue as FrameworkElement);
                panel.AttachToTarget(e.NewValue as FrameworkElement);
                panel.Refresh();
            }
        }

        /// <summary>
        /// Handles changes to the AutoHideWhenValid property.
        /// </summary>
        private static void OnAutoHideWhenValidChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ValidationSummaryPanel panel)
            {
                panel.UpdateVisibility();
            }
        }

        /// <summary>
        /// Attaches event handlers to the target container.
        /// </summary>
        private void AttachToTarget(FrameworkElement? target)
        {
            if (target == null) return;

            // Subscribe to validation error events
            Validation.AddErrorHandler(target, OnValidationError);
        }

        /// <summary>
        /// Detaches event handlers from the target container.
        /// </summary>
        private void DetachFromTarget(FrameworkElement? target)
        {
            if (target == null) return;

            Validation.RemoveErrorHandler(target, OnValidationError);
        }

        /// <summary>
        /// Handles validation error events.
        /// </summary>
        private void OnValidationError(object? sender, ValidationErrorEventArgs e)
        {
            Refresh();
        }

        /// <summary>
        /// Refreshes the validation summary by scanning the target for all validation errors.
        /// </summary>
        public void Refresh()
        {
            Errors.Clear();

            if (Target == null) return;

            CollectValidationErrors(Target);
            UpdateErrorState();
        }

        /// <summary>
        /// Recursively collects validation errors from the visual tree.
        /// </summary>
        private void CollectValidationErrors(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is FrameworkElement element)
                {
                    // Check for WPF validation errors
                    var errors = Validation.GetErrors(element);
                    foreach (var error in errors)
                    {
                        var propertyName = GetPropertyNameFromBinding(element, error);
                        
                        Errors.Add(new ValidationError
                        {
                            Message = error.ErrorContent?.ToString() ?? "Validation error",
                            PropertyName = propertyName,
                            SourceControl = element
                        });
                    }
                }

                // Recurse into children
                CollectValidationErrors(child);
            }
        }

        /// <summary>
        /// Gets the property name from a binding associated with a validation error.
        /// </summary>
        private static string GetPropertyNameFromBinding(FrameworkElement element, System.Windows.Controls.ValidationError error)
        {
            if (error.BindingInError is BindingExpression bindingExpression)
            {
                var path = bindingExpression.ParentBinding.Path?.Path;
                if (!string.IsNullOrEmpty(path))
                {
                    // Extract just the property name from the path
                    var lastDot = path.LastIndexOf('.');
                    return lastDot >= 0 ? path.Substring(lastDot + 1) : path;
                }
            }

            // Fallback to element name
            return !string.IsNullOrEmpty(element.Name) ? element.Name : element.GetType().Name;
        }

        /// <summary>
        /// Updates the error state properties.
        /// </summary>
        private void UpdateErrorState()
        {
            ErrorCount = Errors.Count;
            HasErrors = Errors.Count > 0;
            UpdateVisibility();
        }

        /// <summary>
        /// Updates the visibility based on error state and AutoHideWhenValid setting.
        /// </summary>
        private void UpdateVisibility()
        {
            if (AutoHideWhenValid)
            {
                Visibility = HasErrors ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Focuses the control associated with the specified validation error.
        /// </summary>
        /// <param name="error">The validation error whose source control should be focused.</param>
        public void FocusError(ValidationError error)
        {
            if (FocusControlOnClick && error.SourceControl != null)
            {
                error.SourceControl.Focus();

                // If it's a TextBox, select all text
                if (error.SourceControl is TextBox textBox)
                {
                    textBox.SelectAll();
                }
            }
        }

        /// <summary>
        /// Manually adds a validation error to the summary.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="propertyName">The property name associated with the error.</param>
        /// <param name="sourceControl">The control associated with the error (optional).</param>
        public void AddError(string message, string propertyName = "", FrameworkElement? sourceControl = null)
        {
            Errors.Add(new ValidationError
            {
                Message = message,
                PropertyName = propertyName,
                SourceControl = sourceControl
            });
        }

        /// <summary>
        /// Clears all manually added errors and refreshes from the target.
        /// </summary>
        public void ClearAndRefresh()
        {
            Errors.Clear();
            Refresh();
        }
    }
}
