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
using System.Windows;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class GridViewBehaviorsExample
    {
        public static readonly DependencyProperty IsAlternatingRowsEnabledProperty = DependencyProperty.Register(
            nameof(IsAlternatingRowsEnabled), typeof(bool), typeof(GridViewBehaviorsExample), new PropertyMetadata(true));

        public bool IsAlternatingRowsEnabled
        {
            get => (bool)GetValue(IsAlternatingRowsEnabledProperty);
            set => SetValue(IsAlternatingRowsEnabledProperty, value);
        }

        public ObservableCollection<GridViewBehaviorItem> Rows { get; } = new()
        {
            new GridViewBehaviorItem("Alpha", Guid.Parse("2f3e4a3f-3f4d-4d16-99b3-2c1ecf2c1a11"), "Primary"),
            new GridViewBehaviorItem("Bravo", Guid.Parse("5df7c8b9-9d10-4e19-9f7b-5d6e4af7e5c2"), "Secondary"),
            new GridViewBehaviorItem("Charlie", Guid.Parse("8a9d9d1c-1a0e-4c2f-8b8a-9c6f6d2b7f33"), "Archive"),
            new GridViewBehaviorItem("Delta", Guid.Parse("96be0376-e0a0-4e7d-affa-614129c8611a"), "Archive"),
            new GridViewBehaviorItem("Echo", Guid.Parse("97e048b8-c168-4979-885e-e1f8026e221c"), "Archive"),
        };

        public GridViewBehaviorsExample()
        {
            InitializeComponent();
            DataContext = this;
        }
    }

    public sealed class GridViewBehaviorItem(string name, Guid identifier, string category)
    {
        public string Name { get; } = name;

        public Guid Identifier { get; } = identifier;

        public string Category { get; } = category;
    }
}
