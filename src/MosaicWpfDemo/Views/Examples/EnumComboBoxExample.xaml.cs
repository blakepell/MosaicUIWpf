/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    /// <summary>
    /// Demo enum showing every display-name behavior: no attribute, EnumDisplayName,
    /// Description, and EnumDisplayName winning when both attributes are present.
    /// </summary>
    public enum ProjectStatus
    {
        NotStarted,

        [EnumDisplayName("In Progress")]
        InProgress,

        [Description("Waiting for Review")]
        AwaitingReview,

        [EnumDisplayName("Completed Successfully")]
        [Description("This description should not be displayed")]
        Completed
    }

    [ObservableObject]
    public partial class EnumComboBoxExample
    {
        [ObservableProperty]
        private ProjectStatus _selectedStatus = ProjectStatus.InProgress;

        [ObservableProperty]
        private ProjectStatus? _optionalStatus;

        public EnumComboBoxExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        [RelayCommand]
        private void ClearOptionalStatus()
        {
            OptionalStatus = null;
        }
    }
}
