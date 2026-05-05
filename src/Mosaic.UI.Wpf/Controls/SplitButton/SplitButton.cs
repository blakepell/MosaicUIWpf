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
    /// Represents a split button with a primary action surface and a separate drop-down surface
    /// that opens the assigned <see cref="FrameworkElement.ContextMenu"/>.
    /// </summary>
    [DefaultEvent(nameof(Click))]
    [DefaultProperty(nameof(Content))]
    [TemplatePart(Name = PartPrimaryButton, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartDropDownButton, Type = typeof(FrameworkElement))]
    public class SplitButton : ContentControl, ICommandSource
    {
        private const string PartPrimaryButton = "PART_PrimaryButton";
        private const string PartDropDownButton = "PART_DropDownButton";

        private bool _updatingDropDownState;

        private FrameworkElement? _primaryButton;
        private FrameworkElement? _dropDownButton;

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="Command"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandProperty = DependencyProperty.Register(
            nameof(Command), typeof(ICommand), typeof(SplitButton), new PropertyMetadata(null, OnCommandChanged));

        /// <summary>
        /// Gets or sets the command to invoke from the primary action surface.
        /// </summary>
        [Category("Action")]
        [Description("Command invoked when the primary action surface is clicked.")]
        public ICommand? Command
        {
            get => (ICommand?)GetValue(CommandProperty);
            set => SetValue(CommandProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandParameter"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandParameterProperty = DependencyProperty.Register(
            nameof(CommandParameter), typeof(object), typeof(SplitButton), new PropertyMetadata(null, OnCommandPropertyChanged));

        /// <summary>
        /// Gets or sets the command parameter passed to <see cref="Command"/>.
        /// </summary>
        [Category("Action")]
        [Description("Optional parameter passed to the primary action command.")]
        public object? CommandParameter
        {
            get => GetValue(CommandParameterProperty);
            set => SetValue(CommandParameterProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommandTarget"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommandTargetProperty = DependencyProperty.Register(
            nameof(CommandTarget), typeof(IInputElement), typeof(SplitButton), new PropertyMetadata(null, OnCommandPropertyChanged));

        /// <summary>
        /// Gets or sets the target element used when <see cref="Command"/> is a routed command.
        /// </summary>
        [Category("Action")]
        [Description("Target used when the primary action command is a routed command.")]
        public IInputElement? CommandTarget
        {
            get => (IInputElement?)GetValue(CommandTargetProperty);
            set => SetValue(CommandTargetProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(SplitButton),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the drop-down context menu is open.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the drop-down context menu is currently open.")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownPlacement"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownPlacementProperty = DependencyProperty.Register(
            nameof(DropDownPlacement), typeof(PlacementMode), typeof(SplitButton), new PropertyMetadata(PlacementMode.Bottom));

        /// <summary>
        /// Gets or sets how the drop-down context menu is positioned relative to the control.
        /// </summary>
        [Category("Layout")]
        [Description("Placement used when opening the drop-down context menu.")]
        public PlacementMode DropDownPlacement
        {
            get => (PlacementMode)GetValue(DropDownPlacementProperty);
            set => SetValue(DropDownPlacementProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownButtonWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownButtonWidthProperty = DependencyProperty.Register(
            nameof(DropDownButtonWidth), typeof(double), typeof(SplitButton), new PropertyMetadata(28.0));

        /// <summary>
        /// Gets or sets the width of the arrow drop-down surface.
        /// </summary>
        [Category("Layout")]
        [Description("Width of the arrow drop-down surface.")]
        public double DropDownButtonWidth
        {
            get => (double)GetValue(DropDownButtonWidthProperty);
            set => SetValue(DropDownButtonWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MatchDropDownWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MatchDropDownWidthProperty = DependencyProperty.Register(
            nameof(MatchDropDownWidth), typeof(bool), typeof(SplitButton), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets a value indicating whether the drop-down menu should be at least as wide as the split button.
        /// </summary>
        [Category("Layout")]
        [Description("When true, the drop-down context menu is at least as wide as the split button.")]
        public bool MatchDropDownWidth
        {
            get => (bool)GetValue(MatchDropDownWidthProperty);
            set => SetValue(MatchDropDownWidthProperty, value);
        }

        private static readonly DependencyPropertyKey IsPrimaryActionEnabledPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsPrimaryActionEnabled), typeof(bool), typeof(SplitButton), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="IsPrimaryActionEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsPrimaryActionEnabledProperty = IsPrimaryActionEnabledPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the primary action can currently be invoked.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the primary action can currently be invoked.")]
        public bool IsPrimaryActionEnabled
        {
            get => (bool)GetValue(IsPrimaryActionEnabledProperty);
            private set => SetValue(IsPrimaryActionEnabledPropertyKey, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="Click"/> routed event.
        /// </summary>
        public static readonly RoutedEvent ClickEvent = ButtonBase.ClickEvent.AddOwner(typeof(SplitButton));

        /// <summary>
        /// Occurs when the primary action surface is clicked.
        /// </summary>
        [Category("Action")]
        [Description("Raised when the primary action surface is clicked.")]
        public event RoutedEventHandler Click
        {
            add => AddHandler(ClickEvent, value);
            remove => RemoveHandler(ClickEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SplitButton));

        /// <summary>
        /// Occurs when the drop-down context menu opens.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the drop-down context menu opens.")]
        public event RoutedEventHandler DropDownOpened
        {
            add => AddHandler(DropDownOpenedEvent, value);
            remove => RemoveHandler(DropDownOpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(SplitButton));

        /// <summary>
        /// Occurs when the drop-down context menu closes.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the drop-down context menu closes.")]
        public event RoutedEventHandler DropDownClosed
        {
            add => AddHandler(DropDownClosedEvent, value);
            remove => RemoveHandler(DropDownClosedEvent, value);
        }

        #endregion

        static SplitButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(typeof(SplitButton)));
            FocusableProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(true));
            ContextMenuProperty.OverrideMetadata(typeof(SplitButton), new FrameworkPropertyMetadata(null, OnContextMenuChanged));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SplitButton"/> class.
        /// </summary>
        public SplitButton()
        {
            Cursor = Cursors.Hand;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            _primaryButton = GetTemplateChild(PartPrimaryButton) as FrameworkElement;
            _dropDownButton = GetTemplateChild(PartDropDownButton) as FrameworkElement;
        }

        /// <summary>
        /// Opens the assigned <see cref="FrameworkElement.ContextMenu"/> below the split button.
        /// </summary>
        public void OpenDropDown()
        {
            if (!CanOpenDropDown())
            {
                SetDropDownOpen(false);
                return;
            }

            var contextMenu = ContextMenu;
            ConfigureContextMenu(contextMenu);
            contextMenu.IsOpen = true;

            if (contextMenu.IsOpen)
            {
                SetDropDownOpen(true);
            }
        }

        /// <summary>
        /// Closes the assigned <see cref="FrameworkElement.ContextMenu"/>.
        /// </summary>
        public void CloseDropDown()
        {
            var contextMenu = ContextMenu;
            if (contextMenu != null)
            {
                contextMenu.IsOpen = false;
            }

            SetDropDownOpen(false);
        }

        private static void OnCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SplitButton splitButton)
            {
                return;
            }

            if (e.OldValue is ICommand oldCommand)
            {
                oldCommand.CanExecuteChanged -= splitButton.CommandCanExecuteChanged;
            }

            if (e.NewValue is ICommand newCommand)
            {
                newCommand.CanExecuteChanged += splitButton.CommandCanExecuteChanged;
            }

            splitButton.UpdateCanExecute();
        }

        private static void OnCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SplitButton splitButton)
            {
                splitButton.UpdateCanExecute();
            }
        }

        private static void OnContextMenuChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SplitButton splitButton)
            {
                return;
            }

            if (e.OldValue is ContextMenu oldContextMenu)
            {
                oldContextMenu.Opened -= splitButton.ContextMenuOpened;
                oldContextMenu.Closed -= splitButton.ContextMenuClosed;
            }

            if (e.NewValue is ContextMenu newContextMenu)
            {
                newContextMenu.Opened += splitButton.ContextMenuOpened;
                newContextMenu.Closed += splitButton.ContextMenuClosed;
            }

            if (splitButton.ContextMenu == null)
            {
                splitButton.SetDropDownOpen(false);
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SplitButton splitButton || splitButton._updatingDropDownState)
            {
                return;
            }

            if ((bool)e.NewValue)
            {
                splitButton.OpenDropDown();
            }
            else
            {
                splitButton.CloseDropDown();
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new SplitButtonAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnContextMenuOpening(ContextMenuEventArgs e)
        {
            if (!CanOpenDropDown())
            {
                e.Handled = true;
                SetDropDownOpen(false);
                return;
            }

            ConfigureContextMenu(ContextMenu);
            base.OnContextMenuOpening(e);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);

            if (!IsEnabled)
            {
                return;
            }

            Focus();
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);

            if (!IsEnabled)
            {
                return;
            }

            if (IsEventSourceWithin(_dropDownButton, e.OriginalSource))
            {
                OpenDropDown();
            }
            else if (IsEventSourceWithin(_primaryButton, e.OriginalSource) || IsEventSourceWithin(this, e.OriginalSource))
            {
                InvokePrimaryAction();
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!IsEnabled)
            {
                return;
            }

            if (e.Key == Key.Escape && IsDropDownOpen)
            {
                CloseDropDown();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F4 || (e.Key == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt))
            {
                OpenDropDown();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                InvokePrimaryAction();
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == IsEnabledProperty)
            {
                UpdateCanExecute();
            }
        }

        internal bool CanInvokePrimaryAction => IsEnabled && IsPrimaryActionEnabled;

        internal void InvokePrimaryActionFromAutomation()
        {
            InvokePrimaryAction();
        }

        internal void ExpandFromAutomation()
        {
            OpenDropDown();
        }

        internal void CollapseFromAutomation()
        {
            CloseDropDown();
        }

        private bool CanOpenDropDown()
        {
            return IsEnabled && ContextMenu is { HasItems: true };
        }

        private void ConfigureContextMenu(ContextMenu? contextMenu)
        {
            if (contextMenu == null)
            {
                return;
            }

            contextMenu.PlacementTarget = this;
            contextMenu.Placement = DropDownPlacement;

            if (MatchDropDownWidth && ActualWidth > 0)
            {
                contextMenu.MinWidth = ActualWidth;
            }
        }

        private void CommandCanExecuteChanged(object? sender, EventArgs e)
        {
            UpdateCanExecute();
        }

        private void UpdateCanExecute()
        {
            IsPrimaryActionEnabled = IsEnabled && CanExecuteCommand();
        }

        private bool CanExecuteCommand()
        {
            var command = Command;
            if (command == null)
            {
                return true;
            }

            var parameter = CommandParameter;
            if (command is RoutedCommand routedCommand)
            {
                return routedCommand.CanExecute(parameter, CommandTarget ?? this);
            }

            return command.CanExecute(parameter);
        }

        private void InvokePrimaryAction()
        {
            if (!CanInvokePrimaryAction)
            {
                return;
            }

            RaiseEvent(new RoutedEventArgs(ClickEvent, this));
            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            var command = Command;
            if (command == null)
            {
                return;
            }

            var parameter = CommandParameter;
            if (command is RoutedCommand routedCommand)
            {
                routedCommand.Execute(parameter, CommandTarget ?? this);
                return;
            }

            command.Execute(parameter);
        }

        private void ContextMenuOpened(object? sender, RoutedEventArgs e)
        {
            var wasOpen = IsDropDownOpen;
            SetDropDownOpen(true);

            if (!wasOpen)
            {
                RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, this));
                RaiseAutomationExpandCollapseChanged(false, true);
            }
        }

        private void ContextMenuClosed(object? sender, RoutedEventArgs e)
        {
            var wasOpen = IsDropDownOpen;
            SetDropDownOpen(false);

            if (wasOpen)
            {
                RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
                RaiseAutomationExpandCollapseChanged(true, false);
            }
        }

        private void SetDropDownOpen(bool isOpen)
        {
            try
            {
                _updatingDropDownState = true;
                SetCurrentValue(IsDropDownOpenProperty, isOpen);
            }
            finally
            {
                _updatingDropDownState = false;
            }
        }

        private void RaiseAutomationExpandCollapseChanged(bool oldValue, bool newValue)
        {
            if (UIElementAutomationPeer.FromElement(this) is SplitButtonAutomationPeer peer)
            {
                peer.RaiseExpandCollapseStateChanged(oldValue, newValue);
            }
        }

        private static bool IsEventSourceWithin(DependencyObject? container, object originalSource)
        {
            if (container == null || originalSource is not DependencyObject source)
            {
                return false;
            }

            DependencyObject? current = source;
            while (current != null)
            {
                if (ReferenceEquals(current, container))
                {
                    return true;
                }

                current = GetVisualOrLogicalParent(current);
            }

            return false;
        }

        private static DependencyObject? GetVisualOrLogicalParent(DependencyObject source)
        {
            if (source is Visual or System.Windows.Media.Media3D.Visual3D)
            {
                return VisualTreeHelper.GetParent(source);
            }

            if (source is FrameworkContentElement frameworkContentElement)
            {
                return frameworkContentElement.Parent;
            }

            return source is FrameworkElement frameworkElement ? frameworkElement.Parent : null;
        }
    }
}
