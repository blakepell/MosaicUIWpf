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

using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A Ribbon command button with large, medium and small presentations.
    /// </summary>
    [DefaultEvent(nameof(Click))]
    [DefaultProperty(nameof(Label))]
    public class RibbonButton : Button, IRibbonControl
    {
        #region Shared Ribbon properties

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="LargeImage"/> dependency property.</summary>
        public static readonly DependencyProperty LargeImageProperty = RibbonItem.LargeImageProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="UseAdaptiveSmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty UseAdaptiveSmallImageProperty = RibbonItem.UseAdaptiveSmallImageProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="GlyphBrush"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphBrushProperty = RibbonItem.GlyphBrushProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="PreferredVariant"/> dependency property.</summary>
        public static readonly DependencyProperty PreferredVariantProperty = RibbonItem.PreferredVariantProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonButton));

        /// <summary>Gets or sets the stable command key used by the Quick Access Toolbar and persistence.</summary>
        [Category("Ribbon")]
        [Description("Stable command identity used by the Quick Access Toolbar and persistence.")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the display label.</summary>
        [Category("Ribbon")]
        [Description("The command label.")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the 32×32 image used by the large variant.</summary>
        [Category("Ribbon")]
        public ImageSource? LargeImage { get => (ImageSource?)GetValue(LargeImageProperty); set => SetValue(LargeImageProperty, value); }

        /// <summary>Gets or sets the 16×16 image used by the medium and small variants.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets whether <see cref="SmallImage"/> uses <see cref="AdaptiveImage"/>. The default is <see langword="false"/>.</summary>
        [Category("Appearance")]
        public bool UseAdaptiveSmallImage { get => (bool)GetValue(UseAdaptiveSmallImageProperty); set => SetValue(UseAdaptiveSmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph used when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets an optional glyph color.</summary>
        [Category("Ribbon")]
        public Brush? GlyphBrush { get => (Brush?)GetValue(GlyphBrushProperty); set => SetValue(GlyphBrushProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets or sets the most expanded variant used when space allows.</summary>
        [Category("Ribbon")]
        [Description("The most expanded variant the command uses when space allows.")]
        public VariantSize PreferredVariant { get => (VariantSize)GetValue(PreferredVariantProperty); set => SetValue(PreferredVariantProperty, value); }

        /// <summary>Gets the container-assigned variant.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        #endregion

        /// <summary>
        /// When set, activation is forwarded instead of executing this button's own command. Used by
        /// generated presentations so that every surface runs the source's command policy.
        /// </summary>
        internal Action? ForwardClick { get; set; }

        static RibbonButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonButton), new FrameworkPropertyMetadata(typeof(RibbonButton)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonButton"/> class.
        /// </summary>
        public RibbonButton()
        {
            RibbonScreenTip.AttachDefault(this);
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (ForwardClick != null)
            {
                ForwardClick();
                return;
            }

            base.OnClick();
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        /// <summary>
        /// Activates the button as if clicked, honoring <see cref="UIElement.IsEnabled"/>.
        /// </summary>
        public void PerformClick()
        {
            if (IsEnabled)
            {
                OnClick();
            }
        }

        /// <inheritdoc />
        public bool SupportsLargeVariant => true;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            PerformClick();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            return kind == RibbonPresentationKind.Overflow
                ? RibbonPresentationFactory.CreateMenuItem(this, PerformClick)
                : RibbonPresentationFactory.CreateButton(this, PerformClick);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonButtonAutomationPeer(this);
    }

    /// <summary>
    /// A Ribbon toggle command (for example Bold) whose checked state is shared by every presentation.
    /// </summary>
    [DefaultEvent(nameof(Checked))]
    [DefaultProperty(nameof(IsChecked))]
    public class RibbonToggleButton : System.Windows.Controls.Primitives.ToggleButton, IRibbonControl
    {
        #region Shared Ribbon properties

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="LargeImage"/> dependency property.</summary>
        public static readonly DependencyProperty LargeImageProperty = RibbonItem.LargeImageProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="UseAdaptiveSmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty UseAdaptiveSmallImageProperty = RibbonItem.UseAdaptiveSmallImageProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="GlyphBrush"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphBrushProperty = RibbonItem.GlyphBrushProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="PreferredVariant"/> dependency property.</summary>
        public static readonly DependencyProperty PreferredVariantProperty = RibbonItem.PreferredVariantProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonToggleButton));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the display label.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the large image.</summary>
        [Category("Ribbon")]
        public ImageSource? LargeImage { get => (ImageSource?)GetValue(LargeImageProperty); set => SetValue(LargeImageProperty, value); }

        /// <summary>Gets or sets the small image.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets whether <see cref="SmallImage"/> uses <see cref="AdaptiveImage"/>. The default is <see langword="false"/>.</summary>
        [Category("Appearance")]
        public bool UseAdaptiveSmallImage { get => (bool)GetValue(UseAdaptiveSmallImageProperty); set => SetValue(UseAdaptiveSmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph used when no image is assigned.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets an optional glyph color.</summary>
        [Category("Ribbon")]
        public Brush? GlyphBrush { get => (Brush?)GetValue(GlyphBrushProperty); set => SetValue(GlyphBrushProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets or sets the most expanded variant used when space allows.</summary>
        [Category("Ribbon")]
        public VariantSize PreferredVariant { get => (VariantSize)GetValue(PreferredVariantProperty); set => SetValue(PreferredVariantProperty, value); }

        /// <summary>Gets the container-assigned variant.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        #endregion

        internal Action? ForwardClick { get; set; }

        static RibbonToggleButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonToggleButton), new FrameworkPropertyMetadata(typeof(RibbonToggleButton)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonToggleButton"/> class.
        /// </summary>
        public RibbonToggleButton()
        {
            RibbonScreenTip.AttachDefault(this);
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (ForwardClick != null)
            {
                ForwardClick();
                return;
            }

            base.OnClick();
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        /// <summary>
        /// Toggles the button as if clicked, honoring <see cref="UIElement.IsEnabled"/>.
        /// </summary>
        public void PerformClick()
        {
            if (IsEnabled)
            {
                OnClick();
            }
        }

        /// <inheritdoc />
        public bool SupportsLargeVariant => true;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            PerformClick();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            return kind == RibbonPresentationKind.Overflow
                ? RibbonPresentationFactory.CreateMenuItem(this, PerformClick, this)
                : RibbonPresentationFactory.CreateToggle(this, PerformClick);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonToggleButtonAutomationPeer(this);
    }

    /// <summary>
    /// A Ribbon check box (for example View › Ruler). Check boxes are always laid out as rows.
    /// </summary>
    [DefaultProperty(nameof(IsChecked))]
    public class RibbonCheckBox : CheckBox, IRibbonControl
    {
        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonCheckBox));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the display label.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        internal Action? ForwardClick { get; set; }

        static RibbonCheckBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonCheckBox), new FrameworkPropertyMetadata(typeof(RibbonCheckBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonCheckBox"/> class.
        /// </summary>
        public RibbonCheckBox()
        {
            RibbonScreenTip.AttachDefault(this);
        }

        /// <inheritdoc />
        protected override void OnClick()
        {
            if (ForwardClick != null)
            {
                ForwardClick();
                return;
            }

            base.OnClick();
            RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
        }

        /// <summary>
        /// Toggles the check box as if clicked, honoring <see cref="UIElement.IsEnabled"/>.
        /// </summary>
        public void PerformClick()
        {
            if (IsEnabled)
            {
                OnClick();
            }
        }

        /// <inheritdoc />
        public bool SupportsLargeVariant => false;

        /// <inheritdoc />
        public bool IsKeyTipTarget => true;

        /// <inheritdoc />
        public string? KeyTipLabel => Label;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip()
        {
            PerformClick();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            if (kind == RibbonPresentationKind.Overflow)
            {
                return RibbonPresentationFactory.CreateMenuItem(this, PerformClick, this);
            }

            var box = new RibbonCheckBox { ForwardClick = PerformClick };
            RibbonPresentationFactory.BindVisuals(box, this);
            RibbonPresentationFactory.Bind(box, IsCheckedProperty, this, IsCheckedProperty);
            return box;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonCheckBoxAutomationPeer(this);
    }
}
