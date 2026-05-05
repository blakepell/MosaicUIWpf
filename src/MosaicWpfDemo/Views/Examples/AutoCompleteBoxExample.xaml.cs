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
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class AutoCompleteBoxExample : INotifyPropertyChanged
    {
        private string? _selectedCity;
        private string _citySearchText = string.Empty;
        private ProductOption? _selectedProduct;
        private string _productSearchText = string.Empty;
        private ProductOption? _selectedAsyncProduct;
        private string _asyncSearchText = string.Empty;
        private int _addedCityCount;

        public ObservableCollection<string> CityOptions { get; } = new()
        {
            "Amsterdam",
            "Austin",
            "Boston",
            "Chicago",
            "Cincinnati",
            "Denver",
            "Indianapolis",
            "London",
            "Louisville",
            "New York",
            "Portland",
            "San Francisco",
            "Seattle",
            "Tokyo"
        };

        public List<ProductOption> ProductOptions { get; } = new()
        {
            new("BK-100", "Backlog Board", "Planning"),
            new("DB-210", "Dashboard Canvas", "Reporting"),
            new("ED-315", "Editor Toolkit", "Productivity"),
            new("LG-420", "Log Explorer", "Operations"),
            new("NT-530", "Notification Tray", "Messaging"),
            new("SR-640", "Search Relay", "Search"),
            new("TM-750", "Theme Manager", "Design")
        };

        public AutoCompleteItemsProvider AsyncProductLookup { get; }

        public string? SelectedCity
        {
            get => _selectedCity;
            set => SetField(ref _selectedCity, value);
        }

        public string CitySearchText
        {
            get => _citySearchText;
            set => SetField(ref _citySearchText, value);
        }

        public ProductOption? SelectedProduct
        {
            get => _selectedProduct;
            set => SetField(ref _selectedProduct, value);
        }

        public string ProductSearchText
        {
            get => _productSearchText;
            set => SetField(ref _productSearchText, value);
        }

        public ProductOption? SelectedAsyncProduct
        {
            get => _selectedAsyncProduct;
            set => SetField(ref _selectedAsyncProduct, value);
        }

        public string AsyncSearchText
        {
            get => _asyncSearchText;
            set => SetField(ref _asyncSearchText, value);
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public AutoCompleteBoxExample()
        {
            AsyncProductLookup = LookupProductsAsync;
            DataContext = this;
            InitializeComponent();
        }

        private async Task<System.Collections.IEnumerable?> LookupProductsAsync(string searchText, CancellationToken cancellationToken)
        {
            await Task.Delay(450, cancellationToken);

            return ProductOptions
                .Where(product =>
                    product.Name.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    product.Sku.Contains(searchText, StringComparison.CurrentCultureIgnoreCase) ||
                    product.Category.Contains(searchText, StringComparison.CurrentCultureIgnoreCase))
                .ToList();
        }

        private void AddCity_OnClick(object sender, RoutedEventArgs e)
        {
            var city = _addedCityCount++ == 0 ? "Carmel" : $"Carmel {_addedCityCount}";
            CityOptions.Add(city);
        }

        private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }

            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public sealed class ProductOption
        {
            public string Sku { get; }

            public string Name { get; }

            public string Category { get; }

            public ProductOption(string sku, string name, string category)
            {
                Sku = sku;
                Name = name;
                Category = category;
            }
        }
    }
}
