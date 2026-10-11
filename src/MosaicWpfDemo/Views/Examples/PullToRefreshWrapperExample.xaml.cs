/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MosaicWpfDemo.Views.Examples;

/// <summary>
/// Demonstrates refresh gestures and ordinary item button clicks in a list.
/// </summary>
public partial class PullToRefreshWrapperExample
{
    /// <summary>
    /// Initializes a new instance of the PullToRefreshWrapperExample class.
    /// </summary>
    public PullToRefreshWrapperExample()
    {
        InitializeComponent();
        DataContext = new PullToRefreshWrapperExampleViewModel();
    }
}

/// <summary>
/// Simulates an asynchronous feed refresh with randomly generated items.
/// </summary>
public class PullToRefreshWrapperExampleViewModel : ObservableObject
{
    private bool _isRefreshing;
    private string _status = "Ready — pull down from the top to refresh.";
    private int _sequence;

    /// <summary>
    /// Initializes a new instance of the PullToRefreshWrapperExampleViewModel class.
    /// </summary>
    public PullToRefreshWrapperExampleViewModel()
    {
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        InspectCommand = new RelayCommand<string>(item => Status = $"Clicked: {item}");
        for (var i = 0; i < 24; i++) Items.Add(CreateItem());
    }

    /// <summary>
    /// Gets the simulated feed items.
    /// </summary>
    public ObservableCollection<string> Items { get; } = [];

    /// <summary>
    /// Gets the command that simulates a two-second refresh.
    /// </summary>
    public IAsyncRelayCommand RefreshCommand { get; }

    /// <summary>
    /// Gets the command demonstrating that ordinary item clicks still work.
    /// </summary>
    public IRelayCommand<string> InspectCommand { get; }

    /// <summary>
    /// Gets or sets whether the simulated refresh is running.
    /// </summary>
    public bool IsRefreshing { get => _isRefreshing; set => SetProperty(ref _isRefreshing, value); }

    /// <summary>
    /// Gets or sets the current demo status.
    /// </summary>
    public string Status { get => _status; set => SetProperty(ref _status, value); }

    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        Status = "Fetching new activity…";
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(2));
            var count = Random.Shared.Next(2, 6);
            for (var i = 0; i < count; i++) Items.Insert(0, CreateItem());
            Status = $"Added {count} items at {DateTime.Now:T}. {Items.Count} items total.";
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private string CreateItem()
    {
        string[] names = ["Avery", "Blake", "Jordan", "Morgan", "Riley", "Taylor"];
        string[] actions = ["shared a photo", "posted an update", "added a note", "completed a task", "joined the discussion"];
        return $"#{++_sequence:000}  {names[Random.Shared.Next(names.Length)]} {actions[Random.Shared.Next(actions.Length)]}";
    }
}
