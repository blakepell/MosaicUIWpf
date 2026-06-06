/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class DataGridBehaviorsExample
    {
        public ObservableCollection<DataGridBehaviorItem> Rows { get; } = new()
        {
            new DataGridBehaviorItem("Alpha", "Primary", "Ready"),
            new DataGridBehaviorItem("Bravo", "Secondary", "Pending"),
            new DataGridBehaviorItem("Charlie", "Archive", "Complete")
        };

        public DataGridBehaviorsExample()
        {
            InitializeComponent();
            DataContext = this;
        }
    }

    public sealed class DataGridBehaviorItem(string name, string category, string status)
    {
        public string Name { get; } = name;

        public string Category { get; } = category;

        public string Status { get; } = status;
    }
}
