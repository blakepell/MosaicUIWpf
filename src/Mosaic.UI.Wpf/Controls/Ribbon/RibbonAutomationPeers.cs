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

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    internal static class RibbonAutomationHelper
    {
        public static string Name(string? baseName, DependencyObject owner)
        {
            return !string.IsNullOrEmpty(baseName) ? baseName : RibbonItem.GetLabel(owner) ?? string.Empty;
        }

        public static string Help(string? baseHelp, DependencyObject owner)
        {
            return !string.IsNullOrEmpty(baseHelp) ? baseHelp : RibbonItem.GetDescription(owner) ?? string.Empty;
        }

        public static string AccessKey(string? baseKey, DependencyObject owner)
        {
            var tip = RibbonKeyTipService.GetKeyTip(owner);
            return !string.IsNullOrEmpty(tip) ? tip : baseKey ?? string.Empty;
        }

        public static string Accelerator(string? baseKey, DependencyObject owner)
        {
            return !string.IsNullOrEmpty(baseKey) ? baseKey : RibbonItem.GetKeyGestureText(owner) ?? string.Empty;
        }
    }

    /// <summary>
    /// Exposes <see cref="Ribbon"/> to UI Automation as a tab control with selection.
    /// </summary>
    public class RibbonAutomationPeer : FrameworkElementAutomationPeer, ISelectionProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonAutomationPeer"/> class.</summary>
        public RibbonAutomationPeer(Ribbon owner) : base(owner)
        {
        }

        private Ribbon Ribbon => (Ribbon)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Tab;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(Ribbon);

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? "Ribbon" : name;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.Selection ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public IRawElementProviderSimple[] GetSelection()
        {
            var header = Ribbon.SelectedTab?.TabHeader;
            if (header == null || CreatePeerForElement(header) is not { } peer)
            {
                return Array.Empty<IRawElementProviderSimple>();
            }

            return new[] { ProviderFromPeer(peer) };
        }

        /// <inheritdoc />
        public bool CanSelectMultiple => false;

        /// <inheritdoc />
        public bool IsSelectionRequired => false;
    }

    /// <summary>
    /// Exposes a <see cref="RibbonTabHeader"/> as a selectable tab item.
    /// </summary>
    public class RibbonTabHeaderAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonTabHeaderAutomationPeer"/> class.</summary>
        public RibbonTabHeaderAutomationPeer(RibbonTabHeader owner) : base(owner)
        {
        }

        private RibbonTabHeader Header => (RibbonTabHeader)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonTabHeader);

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? Header.Tab.Header?.ToString() ?? string.Empty : name;
        }

        /// <inheritdoc />
        protected override string GetAccessKeyCore() => RibbonAutomationHelper.AccessKey(base.GetAccessKeyCore(), Header);

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.SelectionItem ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public void Select()
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            Header.Tab.Ribbon?.SelectTab(Header.Tab, openWhenMinimized: true);
        }

        /// <inheritdoc />
        public void AddToSelection() => Select();

        /// <inheritdoc />
        public void RemoveFromSelection()
        {
            throw new InvalidOperationException("A Ribbon always keeps one tab selected while tabs are available.");
        }

        /// <inheritdoc />
        public bool IsSelected => Header.IsSelected;

        /// <inheritdoc />
        public IRawElementProviderSimple? SelectionContainer =>
            Header.Tab.Ribbon is { } ribbon && CreatePeerForElement(ribbon) is { } peer ? ProviderFromPeer(peer) : null;
    }

    /// <summary>
    /// Exposes the selected tab's command area as a pane named after the tab.
    /// </summary>
    public class RibbonTabItemAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonTabItemAutomationPeer"/> class.</summary>
        public RibbonTabItemAutomationPeer(RibbonTabItem owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonTabItem);

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? ((RibbonTabItem)Owner).Header?.ToString() ?? string.Empty : name;
        }
    }

    /// <summary>
    /// Exposes a <see cref="RibbonGroup"/> as a named group; collapsed groups add expand/collapse.
    /// </summary>
    public class RibbonGroupAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonGroupAutomationPeer"/> class.</summary>
        public RibbonGroupAutomationPeer(RibbonGroup owner) : base(owner)
        {
        }

        private RibbonGroup Group => (RibbonGroup)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Group;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonGroup);

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? Group.Header?.ToString() ?? string.Empty : name;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.ExpandCollapse && Group.Variant == VariantSize.Collapsed ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public void Expand() => Group.SetCurrentValue(RibbonGroup.IsDropDownOpenProperty, true);

        /// <inheritdoc />
        public void Collapse() => Group.SetCurrentValue(RibbonGroup.IsDropDownOpenProperty, false);

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState => Group.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    }

    /// <summary>
    /// Exposes a <see cref="RibbonButton"/> with its label, description, key tip and shortcut text.
    /// </summary>
    public class RibbonButtonAutomationPeer : ButtonAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonButtonAutomationPeer"/> class.</summary>
        public RibbonButtonAutomationPeer(RibbonButton owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetHelpTextCore() => RibbonAutomationHelper.Help(base.GetHelpTextCore(), Owner);

        /// <inheritdoc />
        protected override string GetAccessKeyCore() => RibbonAutomationHelper.AccessKey(base.GetAccessKeyCore(), Owner);

        /// <inheritdoc />
        protected override string GetAcceleratorKeyCore() => RibbonAutomationHelper.Accelerator(base.GetAcceleratorKeyCore(), Owner);

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonButton);
    }

    /// <summary>
    /// Exposes a <see cref="RibbonToggleButton"/> with the toggle pattern and Ribbon metadata.
    /// </summary>
    public class RibbonToggleButtonAutomationPeer : ToggleButtonAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonToggleButtonAutomationPeer"/> class.</summary>
        public RibbonToggleButtonAutomationPeer(RibbonToggleButton owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetHelpTextCore() => RibbonAutomationHelper.Help(base.GetHelpTextCore(), Owner);

        /// <inheritdoc />
        protected override string GetAccessKeyCore() => RibbonAutomationHelper.AccessKey(base.GetAccessKeyCore(), Owner);

        /// <inheritdoc />
        protected override string GetAcceleratorKeyCore() => RibbonAutomationHelper.Accelerator(base.GetAcceleratorKeyCore(), Owner);

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonToggleButton);
    }

    /// <summary>
    /// Exposes a <see cref="RibbonCheckBox"/> with the toggle pattern and Ribbon metadata.
    /// </summary>
    public class RibbonCheckBoxAutomationPeer : CheckBoxAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonCheckBoxAutomationPeer"/> class.</summary>
        public RibbonCheckBoxAutomationPeer(RibbonCheckBox owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetHelpTextCore() => RibbonAutomationHelper.Help(base.GetHelpTextCore(), Owner);

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonCheckBox);
    }

    /// <summary>
    /// Exposes a <see cref="RibbonComboBox"/> named by its label.
    /// </summary>
    public class RibbonComboBoxAutomationPeer : ComboBoxAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonComboBoxAutomationPeer"/> class.</summary>
        public RibbonComboBoxAutomationPeer(RibbonComboBox owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetHelpTextCore() => RibbonAutomationHelper.Help(base.GetHelpTextCore(), Owner);

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonComboBox);
    }

    /// <summary>
    /// Exposes a <see cref="RibbonMenuButton"/> as a button with expand/collapse.
    /// </summary>
    public class RibbonMenuButtonAutomationPeer : FrameworkElementAutomationPeer, IExpandCollapseProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonMenuButtonAutomationPeer"/> class.</summary>
        public RibbonMenuButtonAutomationPeer(RibbonMenuButton owner) : base(owner)
        {
        }

        /// <summary>Gets the owning menu button.</summary>
        protected RibbonMenuButton MenuButton => (RibbonMenuButton)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Button;

        /// <inheritdoc />
        protected override string GetClassNameCore() => Owner.GetType().Name;

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetHelpTextCore() => RibbonAutomationHelper.Help(base.GetHelpTextCore(), Owner);

        /// <inheritdoc />
        protected override string GetAccessKeyCore() => RibbonAutomationHelper.AccessKey(base.GetAccessKeyCore(), Owner);

        /// <inheritdoc />
        protected override bool IsKeyboardFocusableCore() => true;

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public void Expand() => MenuButton.SetCurrentValue(RibbonMenuButton.IsDropDownOpenProperty, true);

        /// <inheritdoc />
        public void Collapse() => MenuButton.SetCurrentValue(RibbonMenuButton.IsDropDownOpenProperty, false);

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState => MenuButton.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    }

    /// <summary>
    /// Exposes a <see cref="RibbonSplitButton"/> as a split button: invoke runs the primary action and
    /// expand/collapse controls the drop-down independently.
    /// </summary>
    public class RibbonSplitButtonAutomationPeer : RibbonMenuButtonAutomationPeer, IInvokeProvider, IToggleProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonSplitButtonAutomationPeer"/> class.</summary>
        public RibbonSplitButtonAutomationPeer(RibbonSplitButton owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.SplitButton;

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface switch
            {
                PatternInterface.Invoke => this,
                PatternInterface.Toggle when ((RibbonSplitButton)Owner).IsCheckable => this,
                _ => base.GetPattern(patternInterface)
            };
        }

        /// <inheritdoc />
        public ToggleState ToggleState => ((RibbonSplitButton)Owner).IsChecked ? ToggleState.On : ToggleState.Off;

        /// <inheritdoc />
        public void Toggle()
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            ((RibbonSplitButton)Owner).PerformClick();
        }

        /// <inheritdoc />
        public void Invoke()
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            ((RibbonSplitButton)Owner).PerformClick();
        }
    }

    /// <summary>
    /// Exposes a <see cref="RibbonGallery"/> as a list with selection and expand/collapse of its full view.
    /// </summary>
    public class RibbonGalleryAutomationPeer : ListBoxAutomationPeer, IExpandCollapseProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonGalleryAutomationPeer"/> class.</summary>
        public RibbonGalleryAutomationPeer(RibbonGallery owner) : base(owner)
        {
        }

        private RibbonGallery Gallery => (RibbonGallery)Owner;

        /// <inheritdoc />
        protected override string GetNameCore() => RibbonAutomationHelper.Name(base.GetNameCore(), Owner);

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonGallery);

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.ExpandCollapse ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public void Expand() => Gallery.SetCurrentValue(RibbonGallery.IsDropDownOpenProperty, true);

        /// <inheritdoc />
        public void Collapse() => Gallery.SetCurrentValue(RibbonGallery.IsDropDownOpenProperty, false);

        /// <inheritdoc />
        public ExpandCollapseState ExpandCollapseState => Gallery.IsDropDownOpen ? ExpandCollapseState.Expanded : ExpandCollapseState.Collapsed;
    }

    /// <summary>
    /// Exposes the <see cref="RibbonQuickAccessToolbar"/> as a toolbar.
    /// </summary>
    public class RibbonQuickAccessToolbarAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonQuickAccessToolbarAutomationPeer"/> class.</summary>
        public RibbonQuickAccessToolbarAutomationPeer(RibbonQuickAccessToolbar owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ToolBar;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonQuickAccessToolbar);
    }

    /// <summary>
    /// Exposes the <see cref="RibbonBackstage"/> as a window-like pane with selectable pages.
    /// </summary>
    public class RibbonBackstageAutomationPeer : FrameworkElementAutomationPeer
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonBackstageAutomationPeer"/> class.</summary>
        public RibbonBackstageAutomationPeer(RibbonBackstage owner) : base(owner)
        {
        }

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;

        /// <inheritdoc />
        protected override string GetClassNameCore() => nameof(RibbonBackstage);

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? "Backstage view" : name;
        }
    }

    /// <summary>
    /// Exposes a <see cref="RibbonBackstageTabItem"/> as a selectable list item.
    /// </summary>
    public class RibbonBackstageTabItemAutomationPeer : FrameworkElementAutomationPeer, ISelectionItemProvider
    {
        /// <summary>Initializes a new instance of the <see cref="RibbonBackstageTabItemAutomationPeer"/> class.</summary>
        public RibbonBackstageTabItemAutomationPeer(RibbonBackstageTabItem owner) : base(owner)
        {
        }

        private RibbonBackstageTabItem Item => (RibbonBackstageTabItem)Owner;

        /// <inheritdoc />
        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.TabItem;

        /// <inheritdoc />
        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return string.IsNullOrEmpty(name) ? Item.Header?.ToString() ?? string.Empty : name;
        }

        /// <inheritdoc />
        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.SelectionItem ? this : base.GetPattern(patternInterface);
        }

        /// <inheritdoc />
        public void Select() => Item.SetCurrentValue(RibbonBackstageTabItem.IsSelectedProperty, true);

        /// <inheritdoc />
        public void AddToSelection() => Select();

        /// <inheritdoc />
        public void RemoveFromSelection()
        {
        }

        /// <inheritdoc />
        public bool IsSelected => Item.IsSelected;

        /// <inheritdoc />
        public IRawElementProviderSimple? SelectionContainer =>
            ItemsControl.ItemsControlFromItemContainer(Item) is { } owner && CreatePeerForElement(owner) is { } peer ? ProviderFromPeer(peer) : null;
    }
}
