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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace BbsNavigator.Views;

/// <summary>
/// Edits a copied, ordered login sequence without persisting plaintext credentials.
/// </summary>
public partial class LoginSequenceWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the LoginSequenceWindow class.
    /// </summary>
    public LoginSequenceWindow(IEnumerable<LoginStep> steps)
    {
        InitializeComponent();
        Steps = new(steps.Select(s => new LoginStep { Action = s.Action, Text = s.Text, Seconds = s.Seconds }));
        if (Steps.Count == 0)
        {
            Steps.Add(new() { Action = LoginAction.WaitForText, Text = "Name:" });
            Steps.Add(new() { Action = LoginAction.SendUsername });
            Steps.Add(new() { Action = LoginAction.Enter });
            Steps.Add(new() { Action = LoginAction.WaitForText, Text = "Password:" });
            Steps.Add(new() { Action = LoginAction.SendPassword });
            Steps.Add(new() { Action = LoginAction.Enter });
        }
        ActionColumn.ItemsSource = Enum.GetValues<LoginAction>();
        DataContext = this;
    }

    /// <summary>
    /// Gets the editable sequence.
    /// </summary>
    public ObservableCollection<LoginStep> Steps { get; }

    private void Add_OnClick(object sender, RoutedEventArgs e) => Steps.Add(new());
    private void Remove_OnClick(object sender, RoutedEventArgs e)
    {
        if (StepsGrid.SelectedItem is LoginStep step) Steps.Remove(step);
    }
    private void Up_OnClick(object sender, RoutedEventArgs e) => Move(-1);
    private void Down_OnClick(object sender, RoutedEventArgs e) => Move(1);
    private void Move(int direction)
    {
        int index = StepsGrid.SelectedIndex;
        if (index >= 0 && index + direction >= 0 && index + direction < Steps.Count)
        {
            Steps.Move(index, index + direction);
            StepsGrid.SelectedIndex = index + direction;
        }
    }
    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        if (!StepsGrid.CommitEdit(DataGridEditingUnit.Cell, true) || !StepsGrid.CommitEdit(DataGridEditingUnit.Row, true)) return;
        try { LoginSequenceRunner.Validate(Steps); }
        catch (ArgumentException ex)
        {
            Mosaic.UI.Wpf.Controls.MessageBox.Show(ex.Message, "Login steps", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        DialogResult = true;
    }
}
