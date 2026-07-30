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

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A <see cref="ComboBox"/> that lists the standard <see cref="FontWeight"/> values, each one
    /// rendered in the weight it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The items are the <see cref="FontWeight"/> values supplied by <see cref="FontWeightCatalog"/>,
    /// so the control owns its item generation and <c>ItemsSource</c> should not be set by consumers.
    /// The selection is exposed through two interchangeable, two-way bindable properties:
    /// <see cref="SelectedFontWeight"/> for a <see cref="FontWeight"/> and
    /// <see cref="SelectedFontWeightName"/> for the weight name.  Both stay in sync with each other
    /// and with <c>SelectedItem</c>.
    /// </para>
    /// <para>
    /// Each entry previews itself by default; set <see cref="ShowWeightPreview"/> to <c>false</c> for
    /// plain text.  Assigning an <c>ItemTemplate</c> explicitly overrides whatever
    /// <see cref="ShowWeightPreview"/> would apply.
    /// </para>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:FontWeightComboBox SelectedFontWeight="{Binding HeadingWeight, Mode=TwoWay}" /&gt;
    /// </code>
    /// </example>
    /// </remarks>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(SelectedFontWeight))]
    public class FontWeightComboBox : ComboBox
    {
        /// <summary>
        /// Guards the two-way synchronization between <c>SelectedItem</c>,
        /// <see cref="SelectedFontWeight"/>, and <see cref="SelectedFontWeightName"/>.
        /// </summary>
        private bool _isSynchronizing;

        /// <summary>
        /// Identifies the <see cref="SelectedFontWeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontWeightProperty = DependencyProperty.Register(
            nameof(SelectedFontWeight), typeof(FontWeight), typeof(FontWeightComboBox),
            new FrameworkPropertyMetadata(FontWeights.Normal, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontWeightChanged));

        /// <summary>
        /// Gets or sets the selected font weight.  A weight that is not one of the standard weights
        /// cannot be selected and results in no selection.
        /// </summary>
        [Category("Common")]
        [Description("The selected font weight.")]
        public FontWeight SelectedFontWeight
        {
            get => (FontWeight)GetValue(SelectedFontWeightProperty);
            set => SetValue(SelectedFontWeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedFontWeightName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontWeightNameProperty = DependencyProperty.Register(
            nameof(SelectedFontWeightName), typeof(string), typeof(FontWeightComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontWeightNameChanged));

        /// <summary>
        /// Gets or sets the name of the selected font weight, for example <c>SemiBold</c>.  This is
        /// the convenient binding surface for settings that persist a weight by name.
        /// </summary>
        [Category("Common")]
        [Description("The name of the selected font weight.")]
        public string? SelectedFontWeightName
        {
            get => (string?)GetValue(SelectedFontWeightNameProperty);
            set => SetValue(SelectedFontWeightNameProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowWeightPreview"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowWeightPreviewProperty = DependencyProperty.Register(
            nameof(ShowWeightPreview), typeof(bool), typeof(FontWeightComboBox),
            new FrameworkPropertyMetadata(true, OnShowWeightPreviewChanged));

        /// <summary>
        /// Gets or sets a value indicating whether each weight name is rendered in its own weight.
        /// </summary>
        [Category("Appearance")]
        [Description("Indicates whether each weight name is rendered in its own weight.")]
        public bool ShowWeightPreview
        {
            get => (bool)GetValue(ShowWeightPreviewProperty);
            set => SetValue(ShowWeightPreviewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviewFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewFontSizeProperty = DependencyProperty.Register(
            nameof(PreviewFontSize), typeof(double), typeof(FontWeightComboBox),
            new PropertyMetadata(14.0, OnPreviewFontSizeChanged));

        /// <summary>
        /// Gets or sets the font size used to render the previews when <see cref="ShowWeightPreview"/>
        /// is enabled.
        /// </summary>
        [Category("Appearance")]
        [Description("Font size used to render the weight previews.")]
        public double PreviewFontSize
        {
            get => (double)GetValue(PreviewFontSizeProperty);
            set => SetValue(PreviewFontSizeProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontWeightComboBox"/> class and populates it
        /// with the standard font weights.
        /// </summary>
        public FontWeightComboBox()
        {
            ItemsSource = FontWeightCatalog.Weights;
            ItemTemplate = FontWeightCatalog.CreatePreviewTemplate(PreviewFontSize);

            // Pick up the Mosaic native ComboBox style when it has been opted in (Native=true);
            // otherwise this resolves to the system ComboBox style, so the control always renders
            // exactly like the standard ComboBox in the current theme.
            SetResourceReference(StyleProperty, typeof(ComboBox));

            // Select the default weight so the control never starts out blank.
            SelectedItem = FontWeightCatalog.Resolve(SelectedFontWeight);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_isSynchronizing)
            {
                return;
            }

            try
            {
                _isSynchronizing = true;

                if (SelectedItem is FontWeight fontWeight)
                {
                    SetCurrentValue(SelectedFontWeightProperty, fontWeight);
                    SetCurrentValue(SelectedFontWeightNameProperty, fontWeight.ToString());
                }
                else
                {
                    SetCurrentValue(SelectedFontWeightNameProperty, null);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontWeight"/> into the selection and the weight name.
        /// </summary>
        private static void OnSelectedFontWeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontWeightComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontWeightCatalog.Resolve((FontWeight)e.NewValue);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);
                control.SetCurrentValue(SelectedFontWeightNameProperty, resolved?.ToString() ?? e.NewValue.ToString());
            }
            finally
            {
                control._isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontWeightName"/> into the selection and the weight.
        /// </summary>
        private static void OnSelectedFontWeightNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontWeightComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontWeightCatalog.Find(e.NewValue as string);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);

                if (resolved.HasValue)
                {
                    control.SetCurrentValue(SelectedFontWeightProperty, resolved.Value);
                }
            }
            finally
            {
                control._isSynchronizing = false;
            }
        }

        /// <summary>
        /// Rebuilds the preview template so a new size takes effect while previews are showing.
        /// </summary>
        private static void OnPreviewFontSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontWeightComboBox)d;

            if (control.ShowWeightPreview)
            {
                control.ApplyWeightPreview(true);
            }
        }

        /// <summary>
        /// Applies or removes the weight-preview item template.
        /// </summary>
        private static void OnShowWeightPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FontWeightComboBox)d).ApplyWeightPreview((bool)e.NewValue);
        }

        /// <summary>
        /// Switches between the preview item template and plain-text display.  The plain-text case
        /// relies on the item's <c>ToString()</c>, which is the weight name.
        /// </summary>
        /// <param name="showPreview">Whether the weight-preview template should be applied.</param>
        private void ApplyWeightPreview(bool showPreview)
        {
            if (showPreview)
            {
                ItemTemplate = FontWeightCatalog.CreatePreviewTemplate(PreviewFontSize);
            }
            else
            {
                ClearValue(ItemTemplateProperty);
            }
        }
    }
}
