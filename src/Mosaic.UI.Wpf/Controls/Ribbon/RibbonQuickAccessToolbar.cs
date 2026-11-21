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
using System.Windows.Data;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// The Quick Access Toolbar. It hosts independently parented presentations of Ribbon commands; their
    /// checked and enabled state is bound to the source commands, so it never diverges.
    /// </summary>
    [TemplatePart(Name = PartCustomizeButton, Type = typeof(ButtonBase))]
    public class RibbonQuickAccessToolbar : ItemsControl
    {
        private const string PartCustomizeButton = "PART_CustomizeButton";

        private ButtonBase? _customizeButton;

        /// <summary>
        /// Identifies the <see cref="IsMenuOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMenuOpenProperty = DependencyProperty.Register(
            nameof(IsMenuOpen), typeof(bool), typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether the customize menu is open.
        /// </summary>
        public bool IsMenuOpen
        {
            get => (bool)GetValue(IsMenuOpenProperty);
            set => SetValue(IsMenuOpenProperty, value);
        }

        /// <summary>
        /// Gets the Ribbon that owns the toolbar.
        /// </summary>
        public Ribbon Ribbon { get; }

        static RibbonQuickAccessToolbar()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(typeof(RibbonQuickAccessToolbar)));
            FocusableProperty.OverrideMetadata(typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(false));
            FocusManager.IsFocusScopeProperty.OverrideMetadata(typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(true));
            KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
            KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(RibbonQuickAccessToolbar), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RibbonQuickAccessToolbar"/> class.
        /// </summary>
        /// <param name="ribbon">The owning Ribbon.</param>
        public RibbonQuickAccessToolbar(Ribbon ribbon)
        {
            Ribbon = ribbon;
            System.Windows.Shell.WindowChrome.SetIsHitTestVisibleInChrome(this, true);
            SetBinding(VisibilityProperty, new Binding
            {
                Source = ribbon,
                Path = new PropertyPath(Ribbon.IsQuickAccessToolbarVisibleProperty),
                Converter = new BooleanToVisibilityConverter()
            });

            System.Windows.Automation.AutomationProperties.SetName(this, "Quick Access Toolbar");
            ribbon.QuickAccessToolbarLocationChanged += (_, _) => InvalidateMeasure();
            DependencyPropertyDescriptor.FromProperty(Ribbon.QuickAccessToolbarShowLabelsProperty, typeof(Ribbon))
                ?.AddValueChanged(ribbon, (_, _) => InvalidateMeasure());
        }

        /// <inheritdoc />
        protected override bool IsItemItsOwnContainerOverride(object item) => item is UIElement;

        /// <inheritdoc />
        protected override Size MeasureOverride(Size constraint)
        {
            var variant = Ribbon.QuickAccessToolbarShowLabels ? VariantSize.Medium : VariantSize.Small;
            foreach (var item in Items)
            {
                if (item is UIElement element && RibbonItem.GetVariant(element) != variant)
                {
                    RibbonItem.SetVariant(element, variant);
                }
            }

            return base.MeasureOverride(constraint);
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_customizeButton != null)
            {
                _customizeButton.Click -= OnCustomizeClick;
            }

            base.OnApplyTemplate();
            _customizeButton = GetTemplateChild(PartCustomizeButton) as ButtonBase;
            if (_customizeButton != null)
            {
                _customizeButton.Click += OnCustomizeClick;
            }
        }

        private void OnCustomizeClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_customizeButton == null)
            {
                return;
            }

            var menu = new RibbonContextMenu { PlacementTarget = _customizeButton, Placement = PlacementMode.Bottom };
            menu.Items.Add(new RibbonMenuItem { Header = "Customize Quick Access Toolbar", IsEnabled = false, FontWeight = FontWeights.SemiBold });

            foreach (var key in Ribbon.QuickAccessToolbarKeys.ToList())
            {
                var command = Ribbon.FindCommand(key);
                var item = new RibbonMenuItem { Header = command?.Label ?? key, IsCheckable = true, IsChecked = true };
                item.Click += (_, _) => Ribbon.RemoveFromQuickAccessToolbar(key);
                menu.Items.Add(item);
            }

            menu.Items.Add(new Separator());
            foreach (var item in Ribbon.BuildDisplayMenuItems(includeDensity: false))
            {
                menu.Items.Add(item);
            }

            menu.Closed += (_, _) => SetCurrentValue(IsMenuOpenProperty, false);
            SetCurrentValue(IsMenuOpenProperty, true);
            menu.IsOpen = true;
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer() => new RibbonQuickAccessToolbarAutomationPeer(this);
    }

    /// <summary>
    /// Hosts a <see cref="Ribbon"/> and the application's document content. Ribbons are stacked at the top and
    /// every other child fills the remaining area; the Backstage covers the whole panel.
    /// </summary>
    public class RibbonContainerPanel : Panel
    {
        /// <inheritdoc />
        protected override Size MeasureOverride(Size availableSize)
        {
            double top = 0;
            double width = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child is Ribbon)
                {
                    child.Measure(new Size(availableSize.Width, double.PositiveInfinity));
                    top += child.DesiredSize.Height;
                    width = Math.Max(width, child.DesiredSize.Width);
                }
            }

            var remaining = new Size(availableSize.Width, Math.Max(0, availableSize.Height - top));
            double contentHeight = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child is not Ribbon)
                {
                    child.Measure(remaining);
                    width = Math.Max(width, child.DesiredSize.Width);
                    contentHeight = Math.Max(contentHeight, child.DesiredSize.Height);
                }
            }

            return new Size(
                double.IsInfinity(availableSize.Width) ? width : availableSize.Width,
                double.IsInfinity(availableSize.Height) ? top + contentHeight : availableSize.Height);
        }

        /// <inheritdoc />
        protected override Size ArrangeOverride(Size finalSize)
        {
            double top = 0;
            foreach (UIElement child in InternalChildren)
            {
                if (child is Ribbon)
                {
                    child.Arrange(new Rect(0, top, finalSize.Width, child.DesiredSize.Height));
                    top += child.DesiredSize.Height;
                }
            }

            var content = new Rect(0, top, finalSize.Width, Math.Max(0, finalSize.Height - top));
            foreach (UIElement child in InternalChildren)
            {
                if (child is not Ribbon)
                {
                    child.Arrange(content);
                }
            }

            return finalSize;
        }
    }
}
