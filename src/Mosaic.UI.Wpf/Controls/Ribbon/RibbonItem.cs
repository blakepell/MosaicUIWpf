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
    /// An element that the Ribbon key tip service can show a key tip for and activate.
    /// </summary>
    public interface IRibbonKeyTipTarget
    {
        /// <summary>
        /// Gets a value indicating whether the element currently takes a key tip. When <c>false</c> the
        /// key tip service looks for targets among the element's visual descendants instead.
        /// </summary>
        bool IsKeyTipTarget { get; }

        /// <summary>
        /// Gets the text used to generate a key tip when none is assigned through <see cref="RibbonKeyTipService.KeyTipProperty"/>.
        /// </summary>
        string? KeyTipLabel { get; }

        /// <summary>
        /// Performs the element's key tip action.
        /// </summary>
        /// <returns>
        /// The root of a nested key tip scope to show next (for example an opened popup's content),
        /// or <c>null</c> to leave key tip mode.
        /// </returns>
        UIElement? ActivateKeyTip();
    }

    /// <summary>
    /// The contract shared by every command-bearing Ribbon control. The layout engine, overflow menus,
    /// the Quick Access Toolbar and persistence talk to controls only through this contract.
    /// </summary>
    public interface IRibbonControl : IRibbonKeyTipTarget
    {
        /// <summary>
        /// Gets the stable identity of the command. Keys are persisted (never labels or indices) and must be
        /// unique within a <see cref="Ribbon"/>.
        /// </summary>
        string? Key { get; }

        /// <summary>
        /// Gets the localizable display label.
        /// </summary>
        string? Label { get; }

        /// <summary>
        /// Gets a value indicating whether the control has a <see cref="VariantSize.Large"/> presentation.
        /// Controls that cannot be large (editors, check boxes) are laid out as rows.
        /// </summary>
        bool SupportsLargeVariant { get; }

        /// <summary>
        /// Creates an independently parented presentation bound to this control's shared state, or returns
        /// <c>null</c> when the control cannot be presented on the requested surface.
        /// </summary>
        /// <param name="kind">The surface the presentation is for.</param>
        FrameworkElement? CreatePresentation(RibbonPresentationKind kind);
    }

    /// <summary>
    /// Attached properties shared by all Ribbon commands: identity, labels, images and the
    /// container-managed layout state.
    /// </summary>
    public static class RibbonItem
    {
        #region Identity and content

        /// <summary>
        /// Identifies the Key attached property: the stable command identity.
        /// </summary>
        public static readonly DependencyProperty KeyProperty = DependencyProperty.RegisterAttached(
            "Key", typeof(string), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the stable command key.</summary>
        public static string? GetKey(DependencyObject element) => (string?)element.GetValue(KeyProperty);

        /// <summary>Sets the stable command key.</summary>
        public static void SetKey(DependencyObject element, string? value) => element.SetValue(KeyProperty, value);

        /// <summary>
        /// Identifies the Label attached property.
        /// </summary>
        public static readonly DependencyProperty LabelProperty = DependencyProperty.RegisterAttached(
            "Label", typeof(string), typeof(RibbonItem),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>Gets the display label.</summary>
        public static string? GetLabel(DependencyObject element) => (string?)element.GetValue(LabelProperty);

        /// <summary>Sets the display label.</summary>
        public static void SetLabel(DependencyObject element, string? value) => element.SetValue(LabelProperty, value);

        /// <summary>
        /// Identifies the LargeImage attached property (32×32 DIP image used by the large variant).
        /// </summary>
        public static readonly DependencyProperty LargeImageProperty = DependencyProperty.RegisterAttached(
            "LargeImage", typeof(ImageSource), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the large image.</summary>
        public static ImageSource? GetLargeImage(DependencyObject element) => (ImageSource?)element.GetValue(LargeImageProperty);

        /// <summary>Sets the large image.</summary>
        public static void SetLargeImage(DependencyObject element, ImageSource? value) => element.SetValue(LargeImageProperty, value);

        /// <summary>
        /// Identifies the SmallImage attached property (16×16 DIP image used by the medium and small variants).
        /// </summary>
        public static readonly DependencyProperty SmallImageProperty = DependencyProperty.RegisterAttached(
            "SmallImage", typeof(ImageSource), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the small image.</summary>
        public static ImageSource? GetSmallImage(DependencyObject element) => (ImageSource?)element.GetValue(SmallImageProperty);

        /// <summary>Sets the small image.</summary>
        public static void SetSmallImage(DependencyObject element, ImageSource? value) => element.SetValue(SmallImageProperty, value);

        /// <summary>
        /// Identifies the Glyph attached property: a font icon (for example a Segoe Fluent Icons code point)
        /// used when no image is assigned.
        /// </summary>
        public static readonly DependencyProperty GlyphProperty = DependencyProperty.RegisterAttached(
            "Glyph", typeof(string), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the font icon glyph.</summary>
        public static string? GetGlyph(DependencyObject element) => (string?)element.GetValue(GlyphProperty);

        /// <summary>Sets the font icon glyph.</summary>
        public static void SetGlyph(DependencyObject element, string? value) => element.SetValue(GlyphProperty, value);

        /// <summary>
        /// Identifies the GlyphBrush attached property: an optional color for the glyph. When unset the glyph
        /// uses the control foreground, which keeps high contrast themes legible.
        /// </summary>
        public static readonly DependencyProperty GlyphBrushProperty = DependencyProperty.RegisterAttached(
            "GlyphBrush", typeof(Brush), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the glyph brush.</summary>
        public static Brush? GetGlyphBrush(DependencyObject element) => (Brush?)element.GetValue(GlyphBrushProperty);

        /// <summary>Sets the glyph brush.</summary>
        public static void SetGlyphBrush(DependencyObject element, Brush? value) => element.SetValue(GlyphBrushProperty, value);

        /// <summary>
        /// Identifies the Description attached property: explanatory text used by the generated screen tip.
        /// </summary>
        public static readonly DependencyProperty DescriptionProperty = DependencyProperty.RegisterAttached(
            "Description", typeof(string), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the screen tip description.</summary>
        public static string? GetDescription(DependencyObject element) => (string?)element.GetValue(DescriptionProperty);

        /// <summary>Sets the screen tip description.</summary>
        public static void SetDescription(DependencyObject element, string? value) => element.SetValue(DescriptionProperty, value);

        /// <summary>
        /// Identifies the KeyGestureText attached property: shortcut text displayed in screen tips. Display
        /// text is deliberately separate from shortcut registration; it does not register an input binding.
        /// </summary>
        public static readonly DependencyProperty KeyGestureTextProperty = DependencyProperty.RegisterAttached(
            "KeyGestureText", typeof(string), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the displayed shortcut text.</summary>
        public static string? GetKeyGestureText(DependencyObject element) => (string?)element.GetValue(KeyGestureTextProperty);

        /// <summary>Sets the displayed shortcut text.</summary>
        public static void SetKeyGestureText(DependencyObject element, string? value) => element.SetValue(KeyGestureTextProperty, value);

        /// <summary>
        /// Identifies the SourceKey attached property, set on generated presentations (overflow rows and
        /// Quick Access Toolbar buttons) to the key of the command they present.
        /// </summary>
        public static readonly DependencyProperty SourceKeyProperty = DependencyProperty.RegisterAttached(
            "SourceKey", typeof(string), typeof(RibbonItem), new FrameworkPropertyMetadata(null));

        /// <summary>Gets the key of the command a generated presentation represents.</summary>
        public static string? GetSourceKey(DependencyObject element) => (string?)element.GetValue(SourceKeyProperty);

        /// <summary>Sets the key of the command a generated presentation represents.</summary>
        public static void SetSourceKey(DependencyObject element, string? value) => element.SetValue(SourceKeyProperty, value);

        #endregion

        #region Quick Access Toolbar

        /// <summary>
        /// Identifies the CanAddToQuickAccessToolbar attached property. Set it to <c>false</c> to keep a command
        /// out of the Quick Access Toolbar: the "Add to Quick Access Toolbar" menu entry is not offered, and
        /// <see cref="Ribbon.AddToQuickAccessToolbar"/> and restored preferences skip it. The default is <c>true</c>.
        /// </summary>
        public static readonly DependencyProperty CanAddToQuickAccessToolbarProperty = DependencyProperty.RegisterAttached(
            "CanAddToQuickAccessToolbar", typeof(bool), typeof(RibbonItem), new FrameworkPropertyMetadata(true));

        /// <summary>Gets whether the command may be added to the Quick Access Toolbar.</summary>
        public static bool GetCanAddToQuickAccessToolbar(DependencyObject element) => (bool)element.GetValue(CanAddToQuickAccessToolbarProperty);

        /// <summary>Sets whether the command may be added to the Quick Access Toolbar.</summary>
        public static void SetCanAddToQuickAccessToolbar(DependencyObject element, bool value) => element.SetValue(CanAddToQuickAccessToolbarProperty, value);

        private static readonly DependencyPropertyKey IsInQuickAccessToolbarPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsInQuickAccessToolbar", typeof(bool), typeof(RibbonItem), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the read-only IsInQuickAccessToolbar attached property: <c>true</c> on a source command while
        /// it has a presentation in the Quick Access Toolbar. Styles can use it, for example to badge the command.
        /// </summary>
        public static readonly DependencyProperty IsInQuickAccessToolbarProperty = IsInQuickAccessToolbarPropertyKey.DependencyProperty;

        /// <summary>Gets whether the command is currently in the Quick Access Toolbar.</summary>
        public static bool GetIsInQuickAccessToolbar(DependencyObject element) => (bool)element.GetValue(IsInQuickAccessToolbarProperty);

        internal static void SetIsInQuickAccessToolbar(DependencyObject element, bool value)
        {
            if (value)
            {
                element.SetValue(IsInQuickAccessToolbarPropertyKey, true);
            }
            else
            {
                element.ClearValue(IsInQuickAccessToolbarPropertyKey);
            }
        }

        #endregion

        #region Layout

        /// <summary>
        /// Identifies the Variant attached property. The value is managed by the containing group or tab;
        /// assigning it directly is overwritten on the next layout pass.
        /// </summary>
        public static readonly DependencyProperty VariantProperty = DependencyProperty.RegisterAttached(
            "Variant", typeof(VariantSize), typeof(RibbonItem),
            new FrameworkPropertyMetadata(VariantSize.Medium, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange | FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsDefined<VariantSize>);

        /// <summary>Gets the container-assigned variant.</summary>
        public static VariantSize GetVariant(DependencyObject element) => (VariantSize)element.GetValue(VariantProperty);

        /// <summary>Sets the variant. Intended for Ribbon containers.</summary>
        public static void SetVariant(DependencyObject element, VariantSize value) => element.SetValue(VariantProperty, value);

        /// <summary>
        /// Identifies the PreferredVariant attached property: the most expanded variant a command uses when
        /// space allows.
        /// </summary>
        public static readonly DependencyProperty PreferredVariantProperty = DependencyProperty.RegisterAttached(
            "PreferredVariant", typeof(VariantSize), typeof(RibbonItem),
            new FrameworkPropertyMetadata(VariantSize.Medium, FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsDefined<VariantSize>);

        /// <summary>Gets the preferred variant.</summary>
        public static VariantSize GetPreferredVariant(DependencyObject element) => (VariantSize)element.GetValue(PreferredVariantProperty);

        /// <summary>Sets the preferred variant.</summary>
        public static void SetPreferredVariant(DependencyObject element, VariantSize value) => element.SetValue(PreferredVariantProperty, value);

        /// <summary>
        /// Identifies the VariantBehavior attached property.
        /// </summary>
        public static readonly DependencyProperty VariantBehaviorProperty = DependencyProperty.RegisterAttached(
            "VariantBehavior", typeof(ItemVariantBehavior), typeof(RibbonItem),
            new FrameworkPropertyMetadata(ItemVariantBehavior.All, FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsDefined<ItemVariantBehavior>);

        /// <summary>Gets the variant constraint.</summary>
        public static ItemVariantBehavior GetVariantBehavior(DependencyObject element) => (ItemVariantBehavior)element.GetValue(VariantBehaviorProperty);

        /// <summary>Sets the variant constraint.</summary>
        public static void SetVariantBehavior(DependencyObject element, ItemVariantBehavior value) => element.SetValue(VariantBehaviorProperty, value);

        /// <summary>
        /// Identifies the CollapseBehavior attached property (Simplified mode overflow policy).
        /// </summary>
        public static readonly DependencyProperty CollapseBehaviorProperty = DependencyProperty.RegisterAttached(
            "CollapseBehavior", typeof(ItemCollapseBehavior), typeof(RibbonItem),
            new FrameworkPropertyMetadata(ItemCollapseBehavior.Default, FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsDefined<ItemCollapseBehavior>);

        /// <summary>Gets the overflow policy.</summary>
        public static ItemCollapseBehavior GetCollapseBehavior(DependencyObject element) => (ItemCollapseBehavior)element.GetValue(CollapseBehaviorProperty);

        /// <summary>Sets the overflow policy.</summary>
        public static void SetCollapseBehavior(DependencyObject element, ItemCollapseBehavior value) => element.SetValue(CollapseBehaviorProperty, value);

        private static readonly DependencyPropertyKey IsOverflowedPropertyKey = DependencyProperty.RegisterAttachedReadOnly(
            "IsOverflowed", typeof(bool), typeof(RibbonItem),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsParentMeasure | FrameworkPropertyMetadataOptions.AffectsParentArrange, OnIsOverflowedChanged));

        /// <summary>
        /// Identifies the read-only IsOverflowed attached property: <c>true</c> while a command is presented in
        /// an overflow menu instead of inline.
        /// </summary>
        public static readonly DependencyProperty IsOverflowedProperty = IsOverflowedPropertyKey.DependencyProperty;

        /// <summary>Gets a value indicating whether the command is currently in overflow.</summary>
        public static bool GetIsOverflowed(DependencyObject element) => (bool)element.GetValue(IsOverflowedProperty);

        internal static void SetIsOverflowed(DependencyObject element, bool value)
        {
            if (value)
            {
                element.SetValue(IsOverflowedPropertyKey, true);
            }
            else
            {
                element.ClearValue(IsOverflowedPropertyKey);
            }
        }

        private static void OnIsOverflowedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not UIElement element)
            {
                return;
            }

            // Overflowed commands keep measuring (the engine needs their width to bring them back) but must
            // neither render nor take focus. Hidden gives exactly that without overwriting the value source.
            if ((bool)e.NewValue)
            {
                element.SetCurrentValue(UIElement.VisibilityProperty, Visibility.Hidden);
            }
            else
            {
                element.InvalidateProperty(UIElement.VisibilityProperty);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets the label used for an element in menus, key tips and automation.
        /// </summary>
        internal static string? ResolveLabel(DependencyObject element)
        {
            if (element is IRibbonControl rc && !string.IsNullOrEmpty(rc.Label))
            {
                return rc.Label;
            }

            if (element is IRibbonKeyTipTarget kt && !string.IsNullOrEmpty(kt.KeyTipLabel))
            {
                return kt.KeyTipLabel;
            }

            var label = GetLabel(element);
            if (!string.IsNullOrEmpty(label))
            {
                return label;
            }

            var name = System.Windows.Automation.AutomationProperties.GetName(element);
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            return (element as HeaderedItemsControl)?.Header as string ?? (element as ContentControl)?.Content as string;
        }

        /// <summary>
        /// Returns whether an element can be shown at the given variant.
        /// </summary>
        internal static bool Supports(UIElement element, VariantSize variant)
        {
            return variant switch
            {
                VariantSize.Large => element is IRibbonControl { SupportsLargeVariant: true },
                VariantSize.Collapsed => false,
                _ => true
            };
        }

        /// <summary>
        /// Applies the item's <see cref="ItemVariantBehavior"/> and capabilities to a requested variant.
        /// </summary>
        internal static VariantSize Constrain(UIElement element, VariantSize requested)
        {
            var behavior = GetVariantBehavior(element);
            var v = behavior switch
            {
                ItemVariantBehavior.AlwaysLarge => VariantSize.Large,
                ItemVariantBehavior.AlwaysMedium => VariantSize.Medium,
                ItemVariantBehavior.AlwaysSmall => VariantSize.Small,
                ItemVariantBehavior.NeverSmall when requested >= VariantSize.Small => VariantSize.Medium,
                ItemVariantBehavior.NeverMedium when requested == VariantSize.Medium => VariantSize.Small,
                ItemVariantBehavior.NeverLarge when requested == VariantSize.Large => VariantSize.Medium,
                _ => requested
            };

            if (v == VariantSize.Collapsed)
            {
                v = VariantSize.Small;
            }

            if (v == VariantSize.Large && !Supports(element, VariantSize.Large))
            {
                v = VariantSize.Medium;
            }

            return v;
        }

        /// <summary>
        /// Computes a command's variant in Classic mode from its group's content variant.
        /// </summary>
        internal static VariantSize ResolveClassic(UIElement element, VariantSize groupVariant)
        {
            var preferred = GetPreferredVariant(element);
            var requested = (VariantSize)Math.Max((int)preferred, (int)groupVariant);
            return Constrain(element, requested);
        }

        #endregion
    }

    /// <summary>
    /// Dependency property validation callbacks shared by the Ribbon controls. Invalid values are rejected when
    /// they are set (WPF throws <see cref="ArgumentException"/>), so a bad value never reaches layout, where it
    /// would fail later and far from its cause.
    /// </summary>
    internal static class RibbonValidation
    {
        /// <summary>Accepts only named members of <typeparamref name="T"/>.</summary>
        public static bool IsDefined<T>(object value) where T : struct, Enum => value is T t && Enum.IsDefined(t);

        /// <summary>Accepts finite values greater than zero.</summary>
        public static bool IsPositiveFinite(object value) => value is double d && d > 0 && double.IsFinite(d);

        /// <summary>Accepts values greater than zero, including positive infinity (no limit).</summary>
        public static bool IsPositiveOrInfinity(object value) => value is double d && d > 0 && !double.IsNaN(d);

        /// <summary>Accepts finite values of zero or more, or <see cref="double.NaN"/> (automatic size).</summary>
        public static bool IsNonNegativeOrAuto(object value) => value is double d && (double.IsNaN(d) || (d >= 0 && double.IsFinite(d)));

        /// <summary>Accepts integers of one or more.</summary>
        public static bool IsAtLeastOne(object value) => value is int i && i >= 1;

        /// <summary>Accepts non-negative time spans.</summary>
        public static bool IsNonNegativeTimeSpan(object value) => value is TimeSpan t && t >= TimeSpan.Zero;

        /// <summary>Accepts sizes that are not <see cref="Size.Empty"/> and have finite dimensions.</summary>
        public static bool IsFiniteSize(object value) => value is Size s && !s.IsEmpty && double.IsFinite(s.Width) && double.IsFinite(s.Height);
    }

    /// <summary>
    /// Layout metrics shared by Ribbon templates and panels, in device-independent units.
    /// </summary>
    public static class RibbonMetrics
    {
        /// <summary>Vertical gap between Classic rows.</summary>
        public const double RowGap = 2;

        /// <summary>Size of large command icons.</summary>
        public const double LargeIconSize = 32;

        /// <summary>Size of small command icons.</summary>
        public const double SmallIconSize = 16;

        /// <summary>Width reserved at the end of the Simplified row for the tab overflow button.</summary>
        public const double TabOverflowButtonWidth = 40;

        /// <summary>
        /// Gets the height of a Classic command row for a density.
        /// </summary>
        public static double GetRowHeight(UserInterfaceDensity density) => density switch
        {
            UserInterfaceDensity.Compact => 22,
            UserInterfaceDensity.Spacious => 28,
            _ => 24
        };

        /// <summary>
        /// Gets the height of the three-row Classic command area for a density.
        /// </summary>
        public static double GetClassicContentHeight(UserInterfaceDensity density) => (GetRowHeight(density) * 3) + (RowGap * 2);

        /// <summary>
        /// Gets the height of a Simplified command for a density.
        /// </summary>
        public static double GetSimplifiedItemHeight(UserInterfaceDensity density) => density switch
        {
            UserInterfaceDensity.Compact => 28,
            UserInterfaceDensity.Spacious => 36,
            _ => 32
        };
    }
}
