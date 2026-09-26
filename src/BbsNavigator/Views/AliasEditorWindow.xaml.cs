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
using Mosaic.UI.Wpf.Scripting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BbsNavigator.Views;

/// <summary>
/// Edits one alias. The alias passed in is changed as the user types, so callers pass a copy
/// and commit it when <see cref="Window.ShowDialog"/> returns <see langword="true"/>.
/// </summary>
public partial class AliasEditorWindow : Window
{
    private readonly Alias _alias;
    private readonly IReadOnlyList<Alias> _others;

    /// <summary>
    /// Initializes a new instance of the AliasEditorWindow class.
    /// </summary>
    /// <param name="alias">The alias to edit.</param>
    /// <param name="others">The board's other aliases, used to reject duplicate names.</param>
    /// <param name="configureScriptEnvironment">Registers application objects for script completion.</param>
    public AliasEditorWindow(Alias alias, IEnumerable<Alias> others, Action<ScriptEnvironment>? configureScriptEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(alias);
        ArgumentNullException.ThrowIfNull(others);
        InitializeComponent();
        _alias = alias;
        _others = others.ToList();
        Title = string.IsNullOrWhiteSpace(alias.AliasExpression) ? "New alias" : $"Edit alias – {alias.AliasExpression}";

        // The editor only needs completion metadata; aliases run in each terminal's own environment.
        var environment = ScriptEditor.Environment!;
        configureScriptEnvironment?.Invoke(environment);
        environment.RegisterCompletionType("term", typeof(TerminalScriptCommands));

        DataContext = alias;
        Loaded += (_, _) => Keyboard.Focus(string.IsNullOrWhiteSpace(alias.AliasExpression) ? AliasTextBox : ScriptEditor.Editor?.TextArea);
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        string name = _alias.AliasExpression.Trim();
        string? problem = name.Length == 0 ? "Enter the alias name."
            : name.Any(char.IsWhiteSpace) ? "The alias name cannot contain spaces."
            : string.IsNullOrWhiteSpace(_alias.Command) ? "Enter the script to run."
            : HasBindingErrors() ? "Correct the highlighted values."
            : _others.Any(a => string.Equals(a.AliasExpression.Trim(), name, StringComparison.OrdinalIgnoreCase))
                ? $"This board already has an alias named ‘{name}’."
                : null;

        if (problem != null)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(problem, "Alias", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        _alias.AliasExpression = name;
        DialogResult = true;
    }

    private bool HasBindingErrors()
    {
        return LogicalTreeHelper.GetChildren(this).OfType<DependencyObject>().Any(HasErrors);

        static bool HasErrors(DependencyObject element)
        {
            return Validation.GetHasError(element)
                   || LogicalTreeHelper.GetChildren(element).OfType<DependencyObject>().Any(HasErrors);
        }
    }
}
