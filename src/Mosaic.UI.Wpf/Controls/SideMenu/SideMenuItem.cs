/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;
using System.Windows.Markup;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents an item in a hierarchical side menu, supporting child items, commands, and customizable parameters.
    /// </summary>
    [ContentProperty(nameof(Children))]
    public partial class SideMenuItem : ObservableObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether the item is selected.
        /// </summary>
        [ObservableProperty]
        private bool _isSelected;

        /// <summary>
        /// Gets or sets a value indicating whether the current item is expanded.
        /// </summary>
        [ObservableProperty]
        private bool _isExpanded;

        /// <summary>
        /// Gets or sets the text value.
        /// </summary>
        [ObservableProperty]
        private string _text = string.Empty;

        /// <summary>
        /// Gets or sets the header content associated with this object.
        /// </summary>
        [ObservableProperty]
        private object? _header;

        /// <summary>
        /// Gets or sets the command to be executed.
        /// </summary>
        [ObservableProperty]
        private ICommand? _command;

        /// <summary>
        /// Gets or sets the parameter to be passed to the command when it is executed.
        /// </summary>
        [ObservableProperty]
        private object? _commandParameter;

        /// <summary>
        /// Gets or sets an optional tag object associated with this instance.
        /// </summary>
        [ObservableProperty]
        private object? _tag;

        /// <summary>
        /// Gets or sets the collection of child menu items.
        /// </summary>
        [NotifyPropertyChangedFor(nameof(HasChildren))]
        [ObservableProperty]
        private ObservableCollection<SideMenuItem> _children = new();

        /// <summary>
        /// Gets or sets the type of content associated with the current instance.
        /// </summary>
        [ObservableProperty]
        private Type? _contentType;

        /// <summary>
        /// Gets or sets a value indicating whether the content type is a singleton.
        /// </summary>
        [ObservableProperty]
        private bool _contentTypeIsSingleton = true;

        /// <summary>
        /// Gets or sets the source of the image.
        /// </summary>
        [ObservableProperty]
        private ImageSource? _imageSource;

        /// <summary>
        /// Gets or sets the collection of parameters represented as key-value pairs.
        /// </summary>
        [ObservableProperty]
        private Dictionary<string, object?> _parameters = new();

        /// <summary>
        /// Represents a collection of parameters used to configure the side menu.
        /// </summary>
        /// <remarks>This field holds an instance of <see cref="SideMenuParameterCollection"/> that may be
        /// null. It is intended to store configuration details for the side menu, such as layout or behavior
        /// settings.</remarks>
        private SideMenuParameterCollection? _parameterCollection;

        /// <summary>
        /// Collection of parameters that can be set from XAML using nested syntax
        /// </summary>
        public SideMenuParameterCollection? ParameterCollection
        {
            get => _parameterCollection;
            set
            {
                _parameterCollection = value;
                if (value != null)
                {
                    // Merge the collection parameters into the main Parameters dictionary
                    var dict = value.ToDictionary();
                    foreach (var kvp in dict)
                    {
                        SetParameter(kvp.Key, kvp.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current object has any child elements.
        /// </summary>
        public bool HasChildren => Children?.Count > 0;

        public SideMenuItem()
        {

        }

        public SideMenuItem(string text, object? header = null, ICommand? command = null, object? commandParameter = null)
        {
            Text = text;
            Header = header;
            Command = command;
            CommandParameter = commandParameter;

            Children.CollectionChanged += (s, e) => OnPropertyChanged(nameof(HasChildren));
        }

        /// <summary>
        /// Sets a parameter value for this menu item.
        /// </summary>
        /// <param name="key">The parameter key</param>
        /// <param name="value">The parameter value</param>
        /// <returns>This SideMenuItem instance for method chaining</returns>
        public SideMenuItem SetParameter(string key, object? value)
        {
            Parameters[key] = value;
            return this;
        }

        /// <summary>
        /// Gets a parameter value with optional type casting.
        /// </summary>
        /// <typeparam name="T">The expected type of the parameter</typeparam>
        /// <param name="key">The parameter key</param>
        /// <param name="defaultValue">Default value if parameter doesn't exist or can't be cast</param>
        /// <returns>The parameter value or default value</returns>
        public T? GetParameter<T>(string key, T? defaultValue = default)
        {
            if (Parameters.TryGetValue(key, out var value) && value is T typedValue)
            {
                return typedValue;
            }

            return defaultValue;
        }

        /// <summary>
        /// Checks if a parameter exists.
        /// </summary>
        /// <param name="key">The parameter key</param>
        /// <returns>True if the parameter exists</returns>
        public bool HasParameter(string key)
        {
            return Parameters.ContainsKey(key);
        }

        /// <summary>
        /// Removes a parameter.
        /// </summary>
        /// <param name="key">The parameter key</param>
        /// <returns>True if the parameter was removed</returns>
        public bool RemoveParameter(string key)
        {
            return Parameters.Remove(key);
        }

        /// <summary>
        /// Returns a string representation of the current object.
        /// </summary>
        /// <returns>The value of the <see cref="Text"/> property.</returns>
        public override string ToString()
        {
            return this.Text;
        }
    }
}