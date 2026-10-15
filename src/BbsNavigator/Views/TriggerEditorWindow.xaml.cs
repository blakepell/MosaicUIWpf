/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Common;
using BbsNavigator.Models;
using Trigger = BbsNavigator.Models.Trigger;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Mosaic.UI.Wpf.Controls.Scripting;

namespace BbsNavigator.Views;

/// <summary>
/// Edits a trigger copy using the script editor and validates its regex before saving.
/// </summary>
public partial class TriggerEditorWindow : Window
{
    private readonly Trigger _trigger;

    /// <summary>
    /// Initializes a new instance of the TriggerEditorWindow class.
    /// </summary>
    /// <param name="trigger">The editable trigger copy.</param>
    /// <param name="configureScriptEnvironment">The callback that registers application completions.</param>
    public TriggerEditorWindow(Trigger trigger, Action<ScriptEnvironment>? configureScriptEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        InitializeComponent();
        _trigger = trigger;
        Title = string.IsNullOrWhiteSpace(trigger.Name) ? "New trigger" : $"Edit trigger – {trigger.Name}";
        var environment = new ScriptEnvironment();
        configureScriptEnvironment?.Invoke(environment);
        environment.RegisterCompletionType("term", typeof(TerminalScriptCommands));
        ScriptEditor.Environment = environment;
        DataContext = trigger;
        Loaded += (_, _) => Keyboard.Focus(TriggerTextBox);
    }

    /// <summary>
    /// Validates the draft before committing the dialog.
    /// </summary>
    /// <param name="sender">The Save button.</param>
    /// <param name="e">The routed event arguments.</param>
    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        _trigger.GetRegex(out string? regexError);
        string? problem = string.IsNullOrWhiteSpace(_trigger.Name) ? "Enter a trigger name."
            : regexError != null ? regexError
            : string.IsNullOrWhiteSpace(_trigger.Command) ? "Enter the script to run."
            : HasBindingErrors(this) ? "Correct the highlighted values."
            : null;
        if (problem != null)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(problem, "Trigger", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _trigger.Name = _trigger.Name.Trim();
        DialogResult = true;
    }

    /// <summary>
    /// Checks the editor's logical tree for invalid bound values.
    /// </summary>
    /// <param name="element">The root element to inspect.</param>
    /// <returns>True when a binding has a validation error; otherwise, false.</returns>
    private static bool HasBindingErrors(DependencyObject element) =>
        Validation.GetHasError(element)
        || LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>().Any(HasBindingErrors);
}
