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
using Trigger = BbsNavigator.Models.Trigger;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls.Scripting;

namespace BbsNavigator.Views;

/// <summary>
/// Lists, adds, and deletes a board's triggers. Double-click a trigger to edit it.
/// </summary>
public partial class TriggerListWindow : Window
{
    private readonly BbsProfile _profile;
    private readonly Action<ScriptEnvironment>? _configureScriptEnvironment;

    /// <summary>
    /// Initializes a new instance of the TriggerListWindow class.
    /// </summary>
    /// <param name="profile">The board whose triggers are edited in place.</param>
    /// <param name="configureScriptEnvironment">Registers application objects for script completion.</param>
    public TriggerListWindow(BbsProfile profile, Action<ScriptEnvironment>? configureScriptEnvironment = null)
    {
        ArgumentNullException.ThrowIfNull(profile);
        InitializeComponent();
        _profile = profile;
        _configureScriptEnvironment = configureScriptEnvironment;
        Title = $"Triggers – {profile.Name}";
        HeadingText.Text = $"Triggers for {profile.Name}";
        TriggerList.ItemsSource = profile.Triggers;

        var view = CollectionViewSource.GetDefaultView(TriggerList.ItemsSource);
        view.SortDescriptions.Add(new(nameof(Trigger.Priority), ListSortDirection.Ascending));
        view.SortDescriptions.Add(new(nameof(Trigger.Name), ListSortDirection.Ascending));
        UpdateButtons();
    }

    /// <summary>
    /// Adds a trigger after its editor is saved.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Add_OnClick(object sender, RoutedEventArgs e)
    {
        var trigger = new Trigger();
        if (ShowEditor(trigger))
        {
            _profile.Triggers.Add(trigger);
            TriggerList.SelectedItem = trigger;
            TriggerList.ScrollIntoView(trigger);
        }
    }

    /// <summary>
    /// Opens the selected trigger for editing.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Edit_OnClick(object sender, RoutedEventArgs e) => EditSelected();

    /// <summary>
    /// Deletes the selected triggers after confirmation.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void Delete_OnClick(object sender, RoutedEventArgs e) => DeleteSelected();

    /// <summary>
    /// Edits a trigger when its row is double-clicked.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void TriggerList_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        // Ignore double-clicks on headers, scroll bars, and the enabled check box.
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(TriggerList, source) is ListViewItem { Content: Trigger trigger }
            && FindAncestor<CheckBox>(source) == null)
        {
            e.Handled = true;
            Edit(trigger);
        }
    }

    /// <summary>
    /// Finds the nearest ancestor of the specified type.
    /// </summary>
    /// <typeparam name="T">The ancestor type.</typeparam>
    /// <param name="element">The starting element.</param>
    /// <returns>The ancestor, or null when none exists.</returns>
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

    /// <summary>
    /// Handles the Enter and Delete shortcuts.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void TriggerList_OnKeyDown(object sender, KeyEventArgs e)
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

    /// <summary>
    /// Updates actions when the selection changes.
    /// </summary>
    /// <param name="sender">The control raising the event.</param>
    /// <param name="e">The event arguments.</param>
    private void TriggerList_OnSelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateButtons();

    /// <summary>
    /// Enables actions for the current selection.
    /// </summary>
    private void UpdateButtons()
    {
        EditButton.IsEnabled = TriggerList.SelectedItems.Count == 1;
        DeleteButton.IsEnabled = TriggerList.SelectedItems.Count > 0;
    }

    /// <summary>
    /// Edits the sole selected trigger.
    /// </summary>
    private void EditSelected()
    {
        if (TriggerList.SelectedItems.Count == 1 && TriggerList.SelectedItem is Trigger trigger)
        {
            Edit(trigger);
        }
    }

    /// <summary>
    /// Edits a copy of <paramref name="trigger"/> and commits it only when the editor is saved.
    /// </summary>
    /// <param name="trigger">The trigger whose copy is edited.</param>
    private void Edit(Trigger trigger)
    {
        var copy = (Trigger)trigger.Clone();
        if (ShowEditor(copy))
        {
            trigger.CopyFrom(copy);
            CollectionViewSource.GetDefaultView(TriggerList.ItemsSource).Refresh();
            TriggerList.SelectedItem = trigger;
        }
    }

    /// <summary>
    /// Shows the script editor for an editable trigger copy.
    /// </summary>
    /// <param name="trigger">The trigger copy.</param>
    /// <returns>True when the editor was saved; otherwise, false.</returns>
    private bool ShowEditor(Trigger trigger)
    {
        var editor = new TriggerEditorWindow(trigger, _configureScriptEnvironment)
        {
            Owner = this
        };

        return editor.ShowDialog() == true;
    }

    /// <summary>
    /// Confirms and removes the selected triggers.
    /// </summary>
    private void DeleteSelected()
    {
        var selected = TriggerList.SelectedItems.OfType<Trigger>().ToList();
        if (selected.Count == 0)
        {
            return;
        }

        string prompt = selected.Count == 1
            ? $"Delete the trigger ‘{selected[0].Name}’?"
            : $"Delete {selected.Count:N0} triggers?";

        if (Mosaic.UI.Wpf.Controls.MessageBox.Show(prompt, "Triggers", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        foreach (Trigger trigger in selected)
        {
            _profile.Triggers.Remove(trigger);
        }
    }
}
