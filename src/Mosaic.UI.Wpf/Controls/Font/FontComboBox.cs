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
    /// A <see cref="ComboBox"/> that lists every font family installed on the system.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The items are <see cref="FontFamily"/> instances supplied by <see cref="FontFamilyCatalog"/>,
    /// so the control owns its item generation and <c>ItemsSource</c> should not be set by consumers.
    /// The selection is exposed through two interchangeable, two-way bindable properties:
    /// <see cref="SelectedFontFamily"/> for a <see cref="FontFamily"/> and
    /// <see cref="SelectedFontName"/> for the family name.  Both stay in sync with each other and
    /// with <c>SelectedItem</c>.
    /// </para>
    /// <para>
    /// Set <see cref="ShowFontPreview"/> to <c>true</c> to render each font name in its own font (see
    /// <see cref="FontAutoCompleteBox"/> for a control that previews by default).  Assigning an
    /// <c>ItemTemplate</c> explicitly overrides whatever <see cref="ShowFontPreview"/> would apply.
    /// </para>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:FontComboBox SelectedFontFamily="{Binding EditorFont, Mode=TwoWay}" /&gt;
    /// </code>
    /// </example>
    /// </remarks>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(SelectedFontFamily))]
    public class FontComboBox : ComboBox
    {
        /// <summary>
        /// Guards the two-way synchronization between <c>SelectedItem</c>,
        /// <see cref="SelectedFontFamily"/>, and <see cref="SelectedFontName"/>.
        /// </summary>
        private bool _isSynchronizing;

        /// <summary>
        /// Identifies the <see cref="SelectedFontFamily"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontFamilyProperty = DependencyProperty.Register(
            nameof(SelectedFontFamily), typeof(FontFamily), typeof(FontComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontFamilyChanged));

        /// <summary>
        /// Gets or sets the selected font family.  A family that is not installed cannot be selected
        /// and results in no selection.
        /// </summary>
        [Category("Common")]
        [Description("The selected font family.")]
        public FontFamily? SelectedFontFamily
        {
            get => (FontFamily?)GetValue(SelectedFontFamilyProperty);
            set => SetValue(SelectedFontFamilyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedFontName"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedFontNameProperty = DependencyProperty.Register(
            nameof(SelectedFontName), typeof(string), typeof(FontComboBox),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedFontNameChanged));

        /// <summary>
        /// Gets or sets the name of the selected font family, for example <c>Segoe UI</c>.  This is
        /// the convenient binding surface for settings that persist a font by name.
        /// </summary>
        [Category("Common")]
        [Description("The name of the selected font family.")]
        public string? SelectedFontName
        {
            get => (string?)GetValue(SelectedFontNameProperty);
            set => SetValue(SelectedFontNameProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowFontPreview"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowFontPreviewProperty = DependencyProperty.Register(
            nameof(ShowFontPreview), typeof(bool), typeof(FontComboBox),
            new FrameworkPropertyMetadata(false, OnShowFontPreviewChanged));

        /// <summary>
        /// Gets or sets a value indicating whether each font name is rendered in its own font.
        /// </summary>
        [Category("Appearance")]
        [Description("Indicates whether each font name is rendered in its own font.")]
        public bool ShowFontPreview
        {
            get => (bool)GetValue(ShowFontPreviewProperty);
            set => SetValue(ShowFontPreviewProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="PreviewFontSize"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty PreviewFontSizeProperty = DependencyProperty.Register(
            nameof(PreviewFontSize), typeof(double), typeof(FontComboBox),
            new PropertyMetadata(14.0, OnPreviewFontSizeChanged));

        /// <summary>
        /// Gets or sets the font size used to render the previews when <see cref="ShowFontPreview"/>
        /// is enabled.
        /// </summary>
        [Category("Appearance")]
        [Description("Font size used to render the font previews.")]
        public double PreviewFontSize
        {
            get => (double)GetValue(PreviewFontSizeProperty);
            set => SetValue(PreviewFontSizeProperty, value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FontComboBox"/> class and populates it with
        /// the installed font families.
        /// </summary>
        public FontComboBox()
        {
            // TextSearch drives both keyboard type-ahead and the text shown for an item; setting it
            // explicitly keeps that working regardless of whether an item template is in play.
            TextSearch.SetTextPath(this, nameof(FontFamily.Source));

            DisplayMemberPath = nameof(FontFamily.Source);
            SelectedValuePath = nameof(FontFamily.Source);
            ItemsSource = FontFamilyCatalog.Families;

            // Pick up the Mosaic native ComboBox style when it has been opted in (Native=true);
            // otherwise this resolves to the system ComboBox style, so the control always renders
            // exactly like the standard ComboBox in the current theme.
            SetResourceReference(StyleProperty, typeof(ComboBox));
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_isSynchronizing)
            {
                return;
            }

            var fontFamily = SelectedItem as FontFamily;

            try
            {
                _isSynchronizing = true;
                SetCurrentValue(SelectedFontFamilyProperty, fontFamily);
                SetCurrentValue(SelectedFontNameProperty, fontFamily?.Source);
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontFamily"/> into the selection and the font name.
        /// </summary>
        private static void OnSelectedFontFamilyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontFamilyCatalog.Resolve(e.NewValue as FontFamily);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);
                control.SetCurrentValue(SelectedFontNameProperty, resolved?.Source ?? (e.NewValue as FontFamily)?.Source);
            }
            finally
            {
                control._isSynchronizing = false;
            }
        }

        /// <summary>
        /// Pushes a new <see cref="SelectedFontName"/> into the selection and the font family.
        /// </summary>
        private static void OnSelectedFontNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FontComboBox)d;

            if (control._isSynchronizing)
            {
                return;
            }

            var resolved = FontFamilyCatalog.Find(e.NewValue as string);

            try
            {
                control._isSynchronizing = true;
                control.SetCurrentValue(SelectedItemProperty, resolved);
                control.SetCurrentValue(SelectedFontFamilyProperty, resolved);
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
            var control = (FontComboBox)d;

            if (control.ShowFontPreview)
            {
                control.ApplyFontPreview(true);
            }
        }

        /// <summary>
        /// Applies or removes the font-preview item template.
        /// </summary>
        private static void OnShowFontPreviewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((FontComboBox)d).ApplyFontPreview((bool)e.NewValue);
        }

        /// <summary>
        /// Switches between the preview item template and plain-text display.
        /// <c>DisplayMemberPath</c> and <c>ItemTemplate</c> are mutually exclusive in WPF, so
        /// exactly one of them is ever set.
        /// </summary>
        /// <param name="showPreview">Whether the font-preview template should be applied.</param>
        private void ApplyFontPreview(bool showPreview)
        {
            if (showPreview)
            {
                ClearValue(DisplayMemberPathProperty);
                ItemTemplate = FontFamilyCatalog.CreatePreviewTemplate(PreviewFontSize);
            }
            else
            {
                ClearValue(ItemTemplateProperty);
                DisplayMemberPath = nameof(FontFamily.Source);
            }
        }
    }
}
