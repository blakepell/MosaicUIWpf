/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace BbsNavigator.Views;

/// <summary>
/// Command mode: while the command box is shown, keyboard input aimed at the terminal is
/// redirected to the command box so everything typed is composed there before it is sent.
/// </summary>
public partial class BbsTerminalView
{
    private bool _redirectFocusOnMouseUp;

    /// <summary>
    /// Gets whether keyboard input should be redirected from the terminal to the command box.
    /// </summary>
    /// <remarks>
    /// The command box is disabled while disconnected or while a transfer or send is running; the
    /// terminal keeps its normal behavior during those times.
    /// </remarks>
    private bool IsCommandModeActive => Profile.ShowCommandBox && SessionCommandBox.IsEnabled && !_disposed;

    /// <summary>Toggles whether the command box is shown and receives the session's typing.</summary>
    public void ToggleCommandMode()
    {
        Profile.ShowCommandBox = !Profile.ShowCommandBox;

        // Wait for the visibility binding and layout so the newly shown box can accept focus.
        Dispatcher.BeginInvoke(FocusTerminal, DispatcherPriority.Loaded);
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        if (e.Handled)
        {
            return;
        }

        Key key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.Oem2 or Key.Divide && Keyboard.Modifiers == ModifierKeys.Control
            && (Terminal.IsKeyboardFocusWithin || SessionCommandBox.IsKeyboardFocusWithin))
        {
            e.Handled = true;
            ToggleCommandMode();
            return;
        }

        // The view sees the tunneling event before the terminal does, so the key can be taken away
        // from the terminal before it is sent to the connection.
        if (!IsCommandModeActive || !IsTerminalInputTarget(e.OriginalSource) || IsModifierKey(key))
        {
            return;
        }

        // Copying a terminal selection stays with the terminal.
        if (IsCopyGesture(key) && !Terminal.TextArea.Selection.IsEmpty)
        {
            return;
        }

        FocusCommandBox();

        // A character key is left unhandled so the text input Windows generates from it arrives at
        // the command box, which now has focus. Handling it here would suppress that text.
        if (ProducesText(key))
        {
            return;
        }

        e.Handled = true;
        ReplayKeyOnCommandBox(e);
    }

    private void Terminal_OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Focus inside the terminal's search panel is left alone.
        if (!IsCommandModeActive || !IsTerminalInputTarget(e.NewFocus))
        {
            return;
        }

        // Moving focus mid-click would interrupt a drag selection, so wait for the button release.
        if (Mouse.LeftButton == MouseButtonState.Pressed)
        {
            _redirectFocusOnMouseUp = true;
            return;
        }

        Dispatcher.BeginInvoke(RedirectTerminalFocus, DispatcherPriority.Input);
    }

    private void Terminal_OnMouseLeftButtonUpRedirect(object sender, MouseButtonEventArgs e)
    {
        if (!_redirectFocusOnMouseUp)
        {
            return;
        }

        _redirectFocusOnMouseUp = false;

        // Deferred so the selection is completed and copied before focus moves.
        Dispatcher.BeginInvoke(RedirectTerminalFocus, DispatcherPriority.Input);
    }

    /// <summary>
    /// Moves focus to the command box if the terminal still holds it and command mode is active.
    /// </summary>
    private void RedirectTerminalFocus()
    {
        if (IsCommandModeActive && IsTerminalInputTarget(Keyboard.FocusedElement))
        {
            FocusCommandBox();
        }
    }

    private void FocusCommandBox()
    {
        SessionCommandBox.Focus();
        Keyboard.Focus(SessionCommandBox.TextArea);
    }

    /// <summary>
    /// Gets whether an element is the terminal's own input surface rather than, for example,
    /// its search box.
    /// </summary>
    private bool IsTerminalInputTarget(object? element)
    {
        return ReferenceEquals(element, Terminal) || ReferenceEquals(element, Terminal.TextArea);
    }

    /// <summary>
    /// Sends a key the terminal would have handled to the command box instead, so keys such as
    /// Enter, Backspace, and the arrows act on the command box.
    /// </summary>
    private void ReplayKeyOnCommandBox(KeyEventArgs original)
    {
        var target = SessionCommandBox.TextArea;
        var preview = new KeyEventArgs(original.KeyboardDevice, original.InputSource, original.Timestamp, original.Key)
        {
            RoutedEvent = Keyboard.PreviewKeyDownEvent
        };
        target.RaiseEvent(preview);

        if (preview.Handled)
        {
            return;
        }

        target.RaiseEvent(new KeyEventArgs(original.KeyboardDevice, original.InputSource, original.Timestamp, original.Key)
        {
            RoutedEvent = Keyboard.KeyDownEvent
        });
    }

    private static bool IsModifierKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl or Key.LeftShift or Key.RightShift
            or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin
            or Key.CapsLock or Key.NumLock or Key.Scroll;
    }

    private static bool IsCopyGesture(Key key)
    {
        return Keyboard.Modifiers == ModifierKeys.Control && key is Key.C or Key.Insert;
    }

    /// <summary>
    /// Gets whether a key press will produce typed text that the terminal does not act on.
    /// </summary>
    private bool ProducesText(Key key)
    {
        if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt)) != 0)
        {
            return false;
        }

        // With keypad navigation on, the terminal turns the keypad digits into cursor keys.
        if (Terminal.NumericKeypadNavigation && key is >= Key.NumPad0 and <= Key.NumPad9 or Key.Decimal)
        {
            return false;
        }

        return key is Key.Space
            or >= Key.D0 and <= Key.D9
            or >= Key.A and <= Key.Z
            or >= Key.NumPad0 and <= Key.Divide
            or >= Key.Oem1 and <= Key.Oem102
            or Key.ImeProcessed or Key.DeadCharProcessed;
    }
}
