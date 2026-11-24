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
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A Ribbon combo box (for example font name or size). Editors keep keyboard focus while in use.
    /// </summary>
    public class RibbonComboBox : ComboBox, IRibbonControl
    {
        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="SmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty SmallImageProperty = RibbonItem.SmallImageProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="UseAdaptiveSmallImage"/> dependency property.</summary>
        public static readonly DependencyProperty UseAdaptiveSmallImageProperty = RibbonItem.UseAdaptiveSmallImageProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonComboBox));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the label. It is always the automation name, and is displayed when <see cref="ShowLabel"/> is true.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets the small image shown before the label.</summary>
        [Category("Ribbon")]
        public ImageSource? SmallImage { get => (ImageSource?)GetValue(SmallImageProperty); set => SetValue(SmallImageProperty, value); }

        /// <summary>Gets or sets whether <see cref="SmallImage"/> uses <see cref="AdaptiveImage"/>. The default is <see langword="false"/>.</summary>
        [Category("Appearance")]
        public bool UseAdaptiveSmallImage { get => (bool)GetValue(UseAdaptiveSmallImageProperty); set => SetValue(UseAdaptiveSmallImageProperty, value); }

        /// <summary>Gets or sets a font glyph shown before the label.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets the container-assigned variant. The label is hidden at <see cref="VariantSize.Small"/>.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        /// <summary>
        /// Identifies the <see cref="InputWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InputWidthProperty = DependencyProperty.Register(
            nameof(InputWidth), typeof(double), typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(120.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure),
            RibbonValidation.IsNonNegativeOrAuto);

        /// <summary>
        /// Gets or sets the width of the editable box, excluding the label. <see cref="double.NaN"/> sizes it to its content.
        /// </summary>
        [Category("Layout")]
        public double InputWidth
        {
            get => (double)GetValue(InputWidthProperty);
            set => SetValue(InputWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ShowLabel"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowLabelProperty = DependencyProperty.Register(
            nameof(ShowLabel), typeof(bool), typeof(RibbonComboBox),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Gets or sets a value indicating whether the label is displayed beside the box.
        /// </summary>
        [Category("Ribbon")]
        public bool ShowLabel
        {
            get => (bool)GetValue(ShowLabelProperty);
            set => SetValue(ShowLabelProperty, value);
        }

        static RibbonComboBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonComboBox), new FrameworkPropertyMetadata(typeof(RibbonComboBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonComboBox"/> class.
        /// </summary>
        public RibbonComboBox()
        {
            RibbonScreenTip.AttachDefault(this);
        }

        /// <inheritdoc />
        protected override void OnDropDownClosed(EventArgs e)
        {
            base.OnDropDownClosed(e);
            if (InputManager.Current.MostRecentInputDevice is MouseDevice && !IsEditable)
            {
                RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
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
            Focus();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            var clone = new RibbonComboBox
            {
                ShowLabel = false,
                IsEditable = IsEditable,
                InputWidth = InputWidth,
                DisplayMemberPath = DisplayMemberPath,
                ItemTemplate = ItemTemplate,
                ItemsSource = ItemsSource ?? Items.Cast<object>().Select(i => i is ContentControl cc ? cc.Content : i).ToList()
            };

            RibbonPresentationFactory.BindVisuals(clone, this);
            RibbonPresentationFactory.Bind(clone, SelectedIndexProperty, this, SelectedIndexProperty, BindingMode.TwoWay);
            if (IsEditable)
            {
                clone.SetBinding(TextProperty, new Binding { Source = this, Path = new PropertyPath(TextProperty), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.LostFocus });
            }

            return kind == RibbonPresentationKind.Overflow ? RibbonPresentationFactory.CreateEditorRow(this, clone) : clone;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonComboBoxAutomationPeer(this);
    }

    /// <summary>
    /// A Ribbon text box with an optional label.
    /// </summary>
    public class RibbonTextBox : TextBox, IRibbonControl, ICommandSource
    {
        /// <summary>Identifies the <see cref="Command"/> dependency property.</summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(RibbonTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the command executed when the user presses Enter (for example a search or "Go to" box).
        /// The parameter is <see cref="CommandParameter"/> when set, otherwise the current text.
        /// </summary>
        [Category("Action")]
        public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }

        /// <summary>Identifies the <see cref="CommandParameter"/> dependency property.</summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(RibbonTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>Gets or sets a fixed command parameter. When <c>null</c>, <see cref="TextBox.Text"/> is passed.</summary>
        [Category("Action")]
        public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

        /// <summary>Identifies the <see cref="CommandTarget"/> dependency property.</summary>
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
            nameof(CommandTarget), typeof(IInputElement), typeof(RibbonTextBox), new FrameworkPropertyMetadata(null));

        /// <summary>Gets or sets the routed command target.</summary>
        [Category("Action")]
        public IInputElement? CommandTarget { get => (IInputElement?)GetValue(CommandTargetProperty); set => SetValue(CommandTargetProperty, value); }

        /// <summary>Identifies the <see cref="Key"/> dependency property.</summary>
        public static readonly DependencyProperty KeyProperty = RibbonItem.KeyProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>Identifies the <see cref="Label"/> dependency property.</summary>
        public static readonly DependencyProperty LabelProperty = RibbonItem.LabelProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>Identifies the <see cref="Glyph"/> dependency property.</summary>
        public static readonly DependencyProperty GlyphProperty = RibbonItem.GlyphProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>Identifies the <see cref="Description"/> dependency property.</summary>
        public static readonly DependencyProperty DescriptionProperty = RibbonItem.DescriptionProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>Identifies the <see cref="Variant"/> dependency property.</summary>
        public static readonly DependencyProperty VariantProperty = RibbonItem.VariantProperty.AddOwner(typeof(RibbonTextBox));

        /// <summary>Gets or sets the stable command key.</summary>
        [Category("Ribbon")]
        public string? Key { get => (string?)GetValue(KeyProperty); set => SetValue(KeyProperty, value); }

        /// <summary>Gets or sets the label.</summary>
        [Category("Ribbon")]
        public string? Label { get => (string?)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>Gets or sets a font glyph shown before the label.</summary>
        [Category("Ribbon")]
        public string? Glyph { get => (string?)GetValue(GlyphProperty); set => SetValue(GlyphProperty, value); }

        /// <summary>Gets or sets the screen tip description.</summary>
        [Category("Ribbon")]
        public string? Description { get => (string?)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }

        /// <summary>Gets the container-assigned variant. The label is hidden at <see cref="VariantSize.Small"/>.</summary>
        [Browsable(false)]
        public VariantSize Variant => (VariantSize)GetValue(VariantProperty);

        /// <summary>
        /// Identifies the <see cref="InputWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InputWidthProperty = RibbonComboBox.InputWidthProperty.AddOwner(typeof(RibbonTextBox),
            new FrameworkPropertyMetadata(120.0, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsParentMeasure));

        /// <summary>
        /// Gets or sets the width of the text area, excluding the label.
        /// </summary>
        [Category("Layout")]
        public double InputWidth
        {
            get => (double)GetValue(InputWidthProperty);
            set => SetValue(InputWidthProperty, value);
        }

        static RibbonTextBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTextBox), new FrameworkPropertyMetadata(typeof(RibbonTextBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonTextBox"/> class.
        /// </summary>
        public RibbonTextBox()
        {
            RibbonScreenTip.AttachDefault(this);
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (!e.Handled && e.Key == System.Windows.Input.Key.Enter && !AcceptsReturn && Command != null)
            {
                // Push the text to its binding first so the command sees what the user typed.
                GetBindingExpression(TextProperty)?.UpdateSource();
                RibbonCommandHelper.Execute(Command, CommandParameter ?? Text, CommandTarget, this);
                RaiseEvent(new RoutedEventArgs(Ribbon.CommandInvokedEvent, this));
                e.Handled = true;
                return;
            }

            base.OnKeyDown(e);
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
            Focus();
            SelectAll();
            return null;
        }

        /// <inheritdoc />
        public virtual FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            var clone = new RibbonTextBox { InputWidth = InputWidth };
            RibbonPresentationFactory.BindVisuals(clone, this);
            RibbonPresentationFactory.Bind(clone, CommandProperty, this, CommandProperty);
            RibbonPresentationFactory.Bind(clone, CommandParameterProperty, this, CommandParameterProperty);
            RibbonPresentationFactory.Bind(clone, CommandTargetProperty, this, CommandTargetProperty);
            clone.ClearValue(RibbonItem.LabelProperty);
            clone.SetBinding(TextProperty, new Binding { Source = this, Path = new PropertyPath(TextProperty), Mode = BindingMode.TwoWay, UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged });
            return kind == RibbonPresentationKind.Overflow ? RibbonPresentationFactory.CreateEditorRow(this, clone) : clone;
        }
    }

    /// <summary>
    /// Lays out related commands (for example Bold, Italic, Underline) as one row unit. Children are shown
    /// icon-only unless their <see cref="RibbonItem.VariantBehaviorProperty"/> says otherwise.
    /// </summary>
    public class RibbonControlGroup : ItemsControl, IRibbonControl
    {
        static RibbonControlGroup()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonControlGroup), new FrameworkPropertyMetadata(typeof(RibbonControlGroup)));
            FocusableProperty.OverrideMetadata(typeof(RibbonControlGroup), new FrameworkPropertyMetadata(false));
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            foreach (var item in Items)
            {
                if (item is UIElement element)
                {
                    RibbonItem.SetVariant(element, RibbonItem.Constrain(element, VariantSize.Small));
                }
            }

            return base.MeasureOverride(constraint);
        }

        /// <inheritdoc />
        public string? Key => RibbonItem.GetKey(this);

        /// <inheritdoc />
        public string? Label => RibbonItem.GetLabel(this);

        /// <inheritdoc />
        public bool SupportsLargeVariant => false;

        /// <inheritdoc />
        public bool IsKeyTipTarget => false;

        /// <inheritdoc />
        public string? KeyTipLabel => null;

        /// <inheritdoc />
        public UIElement? ActivateKeyTip() => null;

        /// <inheritdoc />
        public FrameworkElement? CreatePresentation(RibbonPresentationKind kind)
        {
            if (kind != RibbonPresentationKind.Overflow)
            {
                return null;
            }

            var row = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(24, 2, 8, 2) };
            foreach (var item in Items)
            {
                if (item is IRibbonControl rc && rc.CreatePresentation(RibbonPresentationKind.QuickAccessToolbar) is { } presentation)
                {
                    RibbonItem.SetVariant(presentation, VariantSize.Small);
                    row.Children.Add(presentation);
                }
            }

            return row;
        }
    }

    /// <summary>
    /// A vertical separator between columns of commands in a group.
    /// </summary>
    public class RibbonSeparator : Control
    {
        static RibbonSeparator()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonSeparator), new FrameworkPropertyMetadata(typeof(RibbonSeparator)));
            FocusableProperty.OverrideMetadata(typeof(RibbonSeparator), new FrameworkPropertyMetadata(false));
            IsTabStopProperty.OverrideMetadata(typeof(RibbonSeparator), new FrameworkPropertyMetadata(false));
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
    }
}
