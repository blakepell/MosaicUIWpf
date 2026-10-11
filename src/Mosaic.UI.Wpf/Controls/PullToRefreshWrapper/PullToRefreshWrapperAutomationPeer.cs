/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls;

/// <summary>
/// Exposes refresh invocation and refresh status to accessibility clients.
/// </summary>
internal sealed class PullToRefreshWrapperAutomationPeer(PullToRefreshWrapper owner)
    : FrameworkElementAutomationPeer(owner), IInvokeProvider
{
    protected override string GetClassNameCore() => nameof(PullToRefreshWrapper);
    protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.Pane;
    protected override string GetItemStatusCore() => owner.IsRefreshing ? "Refreshing" : "Idle";
    public override object? GetPattern(PatternInterface patternInterface) =>
        patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);

    public void Invoke()
    {
        if (!owner.IsEnabled) throw new ElementNotEnabledException();
        owner.Dispatcher.BeginInvoke(new Action(() => owner.RequestRefresh()));
    }

    internal void RaiseRefreshStateChanged(bool oldValue, bool newValue) =>
        RaisePropertyChangedEvent(AutomationElementIdentifiers.ItemStatusProperty,
            oldValue ? "Refreshing" : "Idle", newValue ? "Refreshing" : "Idle");
}
