/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using BbsNavigator.Models;
using Mosaic.UI.Wpf.Scripting;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace BbsNavigator.Views;

/// <summary>
/// Lists, adds, and deletes a board's aliases. Double-click an alias to edit it.
/// </summary>
public partial class AliasListWindow : Window
{
    private readonly BbsProfile _profile;
    private readonly Action<ScriptEnvironment>? _configureScriptEnvironment;

    /// <summary>
    /// Initializes a new instance of the AliasListWindow class.
    /// </summary>
    /// <param name="profile">The board whose aliases are edited in place.</param>
    /// <param name="configureScriptEnvironment">Registers application objects for script completion.</param>
    public AliasListWindow(BbsProfile profile, Action<ScriptEnvironment>? configureScriptEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        InitializeComponent();
        _profile = profile;
        _configureScriptEnvironment = configureScriptEnvironment;
        Title = $"Aliases – {profile.Name}";
        HeadingText.Text = $"Aliases for {profile.Name}";
        AliasList.ItemsSource = profile.Aliases.AsObservable;

        var view = CollectionViewSource.GetDefaultView(AliasList.ItemsSource);
        view.SortDescriptions.Add(new(nameof(Alias.SortOrder), ListSortDirection.Ascending));
        view.SortDescriptions.Add(new(nameof(Alias.AliasExpression), ListSortDirection.Ascending));
        UpdateButtons();
    }

    private void Add_OnClick(object sender, RoutedEventArgs e)
    {
        var alias = new Alias();
        if (ShowEditor(alias))
        {
            _profile.Aliases.Add(alias);
            AliasList.SelectedItem = alias;
            AliasList.ScrollIntoView(alias);
        }
    }

    private void Edit_OnClick(object sender, RoutedEventArgs e) => EditSelected();

    private void Delete_OnClick(object sender, RoutedEventArgs e) => DeleteSelected();

    private void AliasList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Ignore double-clicks on headers, scroll bars, and the enabled check box.
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(AliasList, source) is ListViewItem { Content: Alias alias }
            && FindAncestor<CheckBox>(source) == null)
        {
            e.Handled = true;
            Edit(alias);
        }
    }

    private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
    {
        while (element != null && element is not T)
        {
            element = element is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(element)
                : LogicalTreeHelper.GetParent(element);
        }

        return element as T;
    }

    private void AliasList_OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete)
        {
            e.Handled = true;
            DeleteSelected();
        }
        else if (e.Key == Key.Enter)
        {
            e.Handled = true;
            EditSelected();
        }
    }

    private void AliasList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    private void UpdateButtons()
    {
        EditButton.IsEnabled = AliasList.SelectedItems.Count == 1;
        DeleteButton.IsEnabled = AliasList.SelectedItems.Count > 0;
    }

    private void EditSelected()
    {
        if (AliasList.SelectedItems.Count == 1 && AliasList.SelectedItem is Alias alias)
        {
            Edit(alias);
        }
    }

    /// <summary>
    /// Edits a copy of <paramref name="alias"/> and commits it only when the editor is saved.
    /// </summary>
    private void Edit(Alias alias)
    {
        var copy = (Alias)alias.Clone();
        if (ShowEditor(copy))
        {
            alias.CopyFrom(copy);
            CollectionViewSource.GetDefaultView(AliasList.ItemsSource).Refresh();
            AliasList.SelectedItem = alias;
        }
    }

    private bool ShowEditor(Alias alias)
    {
        var editor = new AliasEditorWindow(alias, _profile.Aliases.Where(a => a.Id != alias.Id), _configureScriptEnvironment)
        {
            Owner = this
        };

        return editor.ShowDialog() == true;
    }

    private void DeleteSelected()
    {
        var selected = AliasList.SelectedItems.OfType<Alias>().ToList();
        if (selected.Count == 0)
        {
            return;
        }

        string prompt = selected.Count == 1
            ? $"Delete the alias ‘{selected[0].AliasExpression}’?"
            : $"Delete {selected.Count:N0} aliases?";

        if (Mosaic.UI.Wpf.Controls.MessageBox.Show(prompt, "Aliases", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (Alias alias in selected)
        {
            _profile.Aliases.Remove(alias);
        }
    }
}
