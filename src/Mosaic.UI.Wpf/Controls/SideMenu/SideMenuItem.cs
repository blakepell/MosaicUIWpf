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
        /// Gets or sets an explicit, already-instantiated framework object to display as this item's content.
        /// When set, this instance is used verbatim (behaving like a singleton that is reused every time the
        /// item is selected) and takes precedence over <see cref="ContentType"/> reflection-based creation.
        /// </summary>
        [ObservableProperty]
        private object? _content;

        /// <summary>
        /// Gets or sets the type of content to display when this item is selected. The parent
        /// <see cref="SideMenu"/> creates an instance of this type and shows it in its content area.
        /// How that instance is created and how long it lives is governed by
        /// <see cref="ReuseContentInstance"/> and <see cref="ContentTypeIsSingleton"/> (see the remarks
        /// on those members). This property is ignored when <see cref="Content"/> is set, since an
        /// explicit instance always takes precedence over reflection-based creation.
        /// </summary>
        [ObservableProperty]
        private Type? _contentType;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="ContentType"/> should be resolved as an
        /// <b>application-wide</b> singleton through the shared dependency-injection container
        /// (<c>AppServices</c>). Defaults to <see langword="true"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <see langword="true"/>, the first time the item is activated the type is registered as a
        /// singleton in the shared service collection (created via its constructor if it was not already
        /// registered) and thereafter resolved from the container. Because the instance is keyed by type
        /// in the global container, it is shared with <i>every</i> consumer that resolves that same type —
        /// other menu items, view models, or anything else using <c>AppServices</c>. Use this when the view
        /// is genuinely a single, shared application component whose state should be consistent everywhere
        /// it appears.
        /// </para>
        /// <para>
        /// When <see langword="false"/>, a brand-new instance is created on every selection (transient),
        /// so the content is rebuilt from scratch each time the item is shown.
        /// </para>
        /// <para>
        /// This flag has no effect when <see cref="ReuseContentInstance"/> is <see langword="true"/> (that
        /// path is evaluated first and never touches the container) or when <see cref="Content"/> is set.
        /// </para>
        /// </remarks>
        [ObservableProperty]
        private bool _contentTypeIsSingleton = true;

        /// <summary>
        /// Gets or sets a value indicating whether this item should cache and reuse a single instance of
        /// <see cref="ContentType"/> that is <b>local to this menu item</b>. Defaults to
        /// <see langword="false"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// When <see langword="true"/>, the item creates the content the first time it is selected and then
        /// holds onto that same instance for its own reuse on subsequent selections. Unlike
        /// <see cref="ContentTypeIsSingleton"/>, this cache lives on the menu item itself and does <i>not</i>
        /// go through the dependency-injection container, so the instance is <b>not</b> shared with other
        /// items or the rest of the app — two different items pointing at the same
        /// <see cref="ContentType"/> each keep their own reused instance. Use this to preserve a view's
        /// state (scroll position, entered text, etc.) across selections without exposing it globally.
        /// </para>
        /// <para>
        /// This flag takes precedence over <see cref="ContentTypeIsSingleton"/>: when it is
        /// <see langword="true"/> the singleton/DI logic is skipped entirely. It is, in turn, ignored when
        /// <see cref="Content"/> is set.
        /// </para>
        /// <para>
        /// <b>Choosing between them:</b> use <see cref="ReuseContentInstance"/> for a per-item cached view;
        /// use <see cref="ContentTypeIsSingleton"/> for one instance shared app-wide via DI; leave both at
        /// their defaults (<see cref="ReuseContentInstance"/> off, <see cref="ContentTypeIsSingleton"/> on)
        /// to get the common "one shared instance" behavior; and set <see cref="ContentTypeIsSingleton"/> to
        /// <see langword="false"/> with <see cref="ReuseContentInstance"/> off to get a fresh instance every
        /// time. Setting both <see langword="true"/> is redundant — <see cref="ReuseContentInstance"/> wins
        /// and the singleton flag is never consulted.
        /// </para>
        /// </remarks>
        [ObservableProperty]
        private bool _reuseContentInstance;

        /// <summary>
        /// Gets or sets the type of <see cref="System.Windows.Window"/> to show as a dialog when this
        /// item is activated. Null by default (no dialog).
        /// </summary>
        [ObservableProperty]
        private Type? _dialogContentType;

        /// <summary>
        /// Gets or sets a value indicating whether <see cref="DialogContentType"/> is shown modally
        /// (ShowDialog) or as a modeless window (Show). True by default.
        /// </summary>
        [ObservableProperty]
        private bool _dialogIsModal = true;

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
        /// Gets or sets the visibility of the menu item.
        /// </summary>
        [ObservableProperty]
        private Visibility _visibility = Visibility.Visible;

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

        internal object? CachedContentInstance { get; set; }

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
        /// Occurs when this menu item is activated (selected).
        /// </summary>
        public event EventHandler<SideMenuItemClickEventArgs>? Click;

        /// <summary>
        /// Raises the <see cref="Click"/> event for this menu item and returns the event arguments so
        /// callers (such as the parent <see cref="SideMenu"/>) can reuse the same snapshot.
        /// </summary>
        /// <returns>The <see cref="SideMenuItemClickEventArgs"/> that was passed to all event handlers.</returns>
        public SideMenuItemClickEventArgs RaiseClick()
        {
            var args = new SideMenuItemClickEventArgs(this);
            Click?.Invoke(this, args);
            return args;
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
