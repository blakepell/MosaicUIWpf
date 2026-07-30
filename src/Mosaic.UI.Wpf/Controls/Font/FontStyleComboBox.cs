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
    /// A <see cref="ComboBox"/> that lists the <see cref="FontStyle"/> values, each one rendered in
    /// the style it names.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The items are the <see cref="FontStyle"/> values supplied by <see cref="FontStyleCatalog"/>,
    /// so the control owns its item generation and <c>ItemsSource</c> should not be set by consumers.
    /// The selection is exposed through two interchangeable, two-way bindable properties:
    /// <see cref="SelectedFontStyle"/> for a <see cref="FontStyle"/> and
    /// <see cref="SelectedFontStyleName"/> for the style name.  Both stay in sync with each other and
    /// with <c>SelectedItem</c>.
    /// </para>
    /// <para>
    /// Each entry previews itself by default; set <see cref="ShowStylePreview"/> to <c>false</c> for
    /// plain text.  Assigning an <c>ItemTemplate</c> explicitly overrides whatever
    /// <see cref="ShowStylePreview"/> would apply.
    /// </para>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:FontStyleComboBox SelectedFontStyle="{Binding HeadingStyle, Mode=TwoWay}" /&gt;
    /// </code>
    /// </example>
    /// </remarks>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(SelectedFontStyle))]
    public class FontStyleComboBox : ComboBox
    {
        /// <summary>
        /// Guards the two-way synchronization between <c>SelectedItem</c>,
        /// <see cref="SelectedFontStyle"/>, and <see cref="SelectedFontStyleName"/>.
        /// </summary>
        private bool _isSynchronizing;

        /// <summary>
        /// Identifies the <see cref="SelectedFontStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontStyleProperty = DependencyProperty.Register(
            nameof(SelectedFontStyle), typeof(FontStyle), typeof(FontStyleComboBox),
            new FrameworkPropertyMetadata(FontStyles.Normal, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontStyleChanged));

        /// <summary>
        /// Gets or sets the selected font style.  A style that is not one of the known styles cannot
        /// be selected and results in no selection.
        /// </summary>
        [Category("Common")]
        [Description("The selected font style.")]
        public FontStyle SelectedFontStyle
        {
            get => (FontStyle)GetValue(SelectedFontStyleProperty);
            set => SetValue(SelectedFontStyleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedFontStyleName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontStyleNameProperty = DependencyProperty.Register(
            nameof(SelectedFontStyleName), typeof(string), typeof(FontStyleComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontStyleNameChanged));

        /// <summary>
        /// Gets or sets the name of the selected font style, for example <c>Italic</c>.  This is the
        /// convenient binding surface for settings that persist a style by name.
        /// </summary>
        [Category("Common")]
        [Description("The name of the selected font style.")]
        public string? SelectedFontStyleName
        {
            get => (string?)GetValue(SelectedFontStyleNameProperty);
            set => SetValue(SelectedFontStyleNameProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowStylePreview"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowStylePreviewProperty = DependencyProperty.Register(
            nameof(ShowStylePreview), typeof(bool), typeof(FontStyleComboBox),
            new FrameworkPropertyMetadata(true, OnShowStylePreviewChanged));

        /// <summary>
        /// Gets or sets a value indicating whether each style name is rendered in its own style.
        /// </summary>
        [Category("Appearance")]
        [Description("Indicates whether each style name is rendered in its own style.")]
        public bool ShowStylePreview
        {
            get => (bool)GetValue(ShowStylePreviewProperty);
            set => SetValue(ShowStylePreviewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviewFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewFontSizeProperty = DependencyProperty.Register(
            nameof(PreviewFontSize), typeof(double), typeof(FontStyleComboBox),
            new PropertyMetadata(14.0, OnPreviewFontSizeChanged));

        /// <summary>
        /// Gets or sets the font size used to render the previews when <see cref="ShowStylePreview"/>
        /// is enabled.
        /// </summary>
        [Category("Appearance")]
        [Description("Font size used to render the style previews.")]
        public double PreviewFontSize
        {
            get => (double)GetValue(PreviewFontSizeProperty);
            set => SetValue(PreviewFontSizeProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontStyleComboBox"/> class and populates it
        /// with the font styles.
        /// </summary>
        public FontStyleComboBox()
        {
            ItemsSource = FontStyleCatalog.Styles;
            ItemTemplate = FontStyleCatalog.CreatePreviewTemplate(PreviewFontSize);

            // Pick up the Mosaic native ComboBox style when it has been opted in (Native=true);
            // otherwise this resolves to the system ComboBox style, so the control always renders
            // exactly like the standard ComboBox in the current theme.
            SetResourceReference(StyleProperty, typeof(ComboBox));

            // Select the default style so the control never starts out blank.
            SelectedItem = FontStyleCatalog.Resolve(SelectedFontStyle);
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

                if (SelectedItem is FontStyle fontStyle)
                {
                    SetCurrentValue(SelectedFontStyleProperty, fontStyle);
                    SetCurrentValue(SelectedFontStyleNameProperty, fontStyle.ToString());
                }
                else
                {
                    SetCurrentValue(SelectedFontStyleNameProperty, null);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontStyle"/> into the selection and the style name.
        /// </summary>
        private static void OnSelectedFontStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontStyleComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontStyleCatalog.Resolve((FontStyle)e.NewValue);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);
                control.SetCurrentValue(SelectedFontStyleNameProperty, resolved?.ToString() ?? e.NewValue.ToString());
            }
            finally
            {
                control._isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontStyleName"/> into the selection and the style.
        /// </summary>
        private static void OnSelectedFontStyleNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontStyleComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontStyleCatalog.Find(e.NewValue as string);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);

                if (resolved.HasValue)
                {
                    control.SetCurrentValue(SelectedFontStyleProperty, resolved.Value);
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
            var control = (FontStyleComboBox)d;

            if (control.ShowStylePreview)
            {
                control.ApplyStylePreview(true);
            }
        }

        /// <summary>
        /// Applies or removes the style-preview item template.
        /// </summary>
        private static void OnShowStylePreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FontStyleComboBox)d).ApplyStylePreview((bool)e.NewValue);
        }

        /// <summary>
        /// Switches between the preview item template and plain-text display.  The plain-text case
        /// relies on the item's <c>ToString()</c>, which is the style name.
        /// </summary>
        /// <param name="showPreview">Whether the style-preview template should be applied.</param>
        private void ApplyStylePreview(bool showPreview)
        {
            if (showPreview)
            {
                ItemTemplate = FontStyleCatalog.CreatePreviewTemplate(PreviewFontSize);
            }
            else
            {
                ClearValue(ItemTemplateProperty);
            }
        }
    }
}
