/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents an editable selection control that filters suggestions while the user types and commits a selected item.
    /// </summary>
    [DefaultEvent(nameof(SelectionChanged))]
    [DefaultProperty(nameof(ItemsSource))]
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartDropDownButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartListBox, Type = typeof(ListBox))]
    public class AutoCompleteBox : Selector
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartDropDownButton = "PART_DropDownButton";
        private const string PartPopup = "PART_Popup";
        private const string PartListBox = "PART_ListBox";

        private readonly DispatcherTimer _lookupTimer;
        private int _lookupVersion;
        private CancellationTokenSource? _lookupCancellationTokenSource;
        private TextBox? _textBox;
        private ButtonBase? _dropDownButton;
        private Popup? _popup;
        private ListBox? _listBox;
        private INotifyCollectionChanged? _itemsSourceCollectionChanged;
        private bool _isCommittingSelection;
        private bool _isUpdatingText;

        /// <summary>
        /// Identifies the <see cref="Text"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            nameof(Text), typeof(string), typeof(AutoCompleteBox),
            new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

        /// <summary>
        /// Gets or sets the current text in the editable portion of the control.
        /// </summary>
        [Category("Common")]
        [Description("Current text in the editable portion of the control.")]
        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark), typeof(string), typeof(AutoCompleteBox), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Gets or sets the placeholder text displayed when the control is empty.
        /// </summary>
        [Category("Common")]
        [Description("Placeholder text displayed when the control is empty.")]
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = DependencyProperty.Register(
            nameof(IsDropDownOpen), typeof(bool), typeof(AutoCompleteBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsDropDownOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the suggestion popup is open.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the suggestion popup is open.")]
        public bool IsDropDownOpen
        {
            get => (bool)GetValue(IsDropDownOpenProperty);
            set => SetValue(IsDropDownOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinimumPrefixLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumPrefixLengthProperty = DependencyProperty.Register(
            nameof(MinimumPrefixLength), typeof(int), typeof(AutoCompleteBox),
            new PropertyMetadata(1, OnSearchBehaviorChanged, CoerceNonNegativeInt));

        /// <summary>
        /// Gets or sets the minimum number of typed characters required before filtering starts.
        /// </summary>
        [Category("Behavior")]
        [Description("Minimum number of typed characters required before filtering starts.")]
        public int MinimumPrefixLength
        {
            get => (int)GetValue(MinimumPrefixLengthProperty);
            set => SetValue(MinimumPrefixLengthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsTextRequiredForSuggestions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsTextRequiredForSuggestionsProperty = DependencyProperty.Register(
            nameof(IsTextRequiredForSuggestions), typeof(bool), typeof(AutoCompleteBox),
            new PropertyMetadata(true, OnSearchBehaviorChanged));

        /// <summary>
        /// Gets or sets a value indicating whether text is required before suggestions are queried.
        /// </summary>
        [Category("Behavior")]
        [Description("Indicates whether text is required before suggestions are queried.")]
        public bool IsTextRequiredForSuggestions
        {
            get => (bool)GetValue(IsTextRequiredForSuggestionsProperty);
            set => SetValue(IsTextRequiredForSuggestionsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxSuggestionCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxSuggestionCountProperty = DependencyProperty.Register(
            nameof(MaxSuggestionCount), typeof(int), typeof(AutoCompleteBox),
            new PropertyMetadata(25, OnSearchBehaviorChanged, CoercePositiveInt));

        /// <summary>
        /// Gets or sets the maximum number of suggestions shown in the dropdown.
        /// </summary>
        [Category("Behavior")]
        [Description("Maximum number of suggestions shown in the dropdown.")]
        public int MaxSuggestionCount
        {
            get => (int)GetValue(MaxSuggestionCountProperty);
            set => SetValue(MaxSuggestionCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownMaxHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownMaxHeightProperty = DependencyProperty.Register(
            nameof(DropDownMaxHeight), typeof(double), typeof(AutoCompleteBox), new PropertyMetadata(300.0));

        /// <summary>
        /// Gets or sets the maximum height of the suggestion dropdown.
        /// </summary>
        [Category("Layout")]
        [Description("Maximum height of the suggestion dropdown.")]
        public double DropDownMaxHeight
        {
            get => (double)GetValue(DropDownMaxHeightProperty);
            set => SetValue(DropDownMaxHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownButtonWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DropDownButtonWidthProperty = DependencyProperty.Register(
            nameof(DropDownButtonWidth), typeof(double), typeof(AutoCompleteBox), new PropertyMetadata(28.0));

        /// <summary>
        /// Gets or sets the width of the dropdown arrow button.
        /// </summary>
        [Category("Layout")]
        [Description("Width of the dropdown arrow button.")]
        public double DropDownButtonWidth
        {
            get => (double)GetValue(DropDownButtonWidthProperty);
            set => SetValue(DropDownButtonWidthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FilterMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterModeProperty = DependencyProperty.Register(
            nameof(FilterMode), typeof(AutoCompleteBoxFilterMode), typeof(AutoCompleteBox),
            new PropertyMetadata(AutoCompleteBoxFilterMode.Contains, OnSearchBehaviorChanged));

        /// <summary>
        /// Gets or sets the built-in filtering mode.
        /// </summary>
        [Category("Behavior")]
        [Description("Built-in filtering mode used for local item sources.")]
        public AutoCompleteBoxFilterMode FilterMode
        {
            get => (AutoCompleteBoxFilterMode)GetValue(FilterModeProperty);
            set => SetValue(FilterModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="FilterPredicate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FilterPredicateProperty = DependencyProperty.Register(
            nameof(FilterPredicate), typeof(AutoCompleteItemFilter), typeof(AutoCompleteBox),
            new PropertyMetadata(null, OnSearchBehaviorChanged));

        /// <summary>
        /// Gets or sets the custom item filter used when <see cref="FilterMode"/> is <see cref="AutoCompleteBoxFilterMode.Custom"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Custom item filter used when FilterMode is Custom.")]
        public AutoCompleteItemFilter? FilterPredicate
        {
            get => (AutoCompleteItemFilter?)GetValue(FilterPredicateProperty);
            set => SetValue(FilterPredicateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemsProvider"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsProviderProperty = DependencyProperty.Register(
            nameof(ItemsProvider), typeof(AutoCompleteItemsProvider), typeof(AutoCompleteBox),
            new PropertyMetadata(null, OnSearchBehaviorChanged));

        /// <summary>
        /// Gets or sets an asynchronous provider used to look up suggestions.
        /// </summary>
        [Category("Behavior")]
        [Description("Asynchronous provider used to look up suggestions.")]
        public AutoCompleteItemsProvider? ItemsProvider
        {
            get => (AutoCompleteItemsProvider?)GetValue(ItemsProviderProperty);
            set => SetValue(ItemsProviderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LookupDelay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LookupDelayProperty = DependencyProperty.Register(
            nameof(LookupDelay), typeof(TimeSpan), typeof(AutoCompleteBox),
            new PropertyMetadata(TimeSpan.FromMilliseconds(250), OnLookupDelayChanged, CoerceNonNegativeTimeSpan));

        /// <summary>
        /// Gets or sets the debounce delay used before filtering or provider lookups run.
        /// </summary>
        [Category("Behavior")]
        [Description("Debounce delay used before filtering or provider lookups run.")]
        public TimeSpan LookupDelay
        {
            get => (TimeSpan)GetValue(LookupDelayProperty);
            set => SetValue(LookupDelayProperty, value);
        }

        private static readonly DependencyPropertyKey IsLoadingPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsLoading), typeof(bool), typeof(AutoCompleteBox), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsLoading"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty = IsLoadingPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether an asynchronous lookup is running.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether an asynchronous lookup is running.")]
        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            private set => SetValue(IsLoadingPropertyKey, value);
        }

        private static readonly DependencyPropertyKey LookupExceptionPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(LookupException), typeof(Exception), typeof(AutoCompleteBox), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="LookupException"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LookupExceptionProperty = LookupExceptionPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets the last exception thrown by <see cref="ItemsProvider"/>.
        /// </summary>
        [Category("Common")]
        [Description("Last exception thrown by the ItemsProvider.")]
        public Exception? LookupException
        {
            get => (Exception?)GetValue(LookupExceptionProperty);
            private set => SetValue(LookupExceptionPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="LoadingContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LoadingContentProperty = DependencyProperty.Register(
            nameof(LoadingContent), typeof(object), typeof(AutoCompleteBox), new PropertyMetadata("Loading..."));

        /// <summary>
        /// Gets or sets the content shown while an asynchronous lookup is running.
        /// </summary>
        [Category("Common")]
        [Description("Content shown while an asynchronous lookup is running.")]
        public object? LoadingContent
        {
            get => GetValue(LoadingContentProperty);
            set => SetValue(LoadingContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="NoResultsContent"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty NoResultsContentProperty = DependencyProperty.Register(
            nameof(NoResultsContent), typeof(object), typeof(AutoCompleteBox), new PropertyMetadata("No results"));

        /// <summary>
        /// Gets or sets the content shown when a search returns no suggestions.
        /// </summary>
        [Category("Common")]
        [Description("Content shown when a search returns no suggestions.")]
        public object? NoResultsContent
        {
            get => GetValue(NoResultsContentProperty);
            set => SetValue(NoResultsContentProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoCompleteBox));

        /// <summary>
        /// Occurs when the suggestion dropdown opens.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the suggestion dropdown opens.")]
        public event RoutedEventHandler DropDownOpened
        {
            add => AddHandler(DropDownOpenedEvent, value);
            remove => RemoveHandler(DropDownOpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(AutoCompleteBox));

        /// <summary>
        /// Occurs when the suggestion dropdown closes.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the suggestion dropdown closes.")]
        public event RoutedEventHandler DropDownClosed
        {
            add => AddHandler(DropDownClosedEvent, value);
            remove => RemoveHandler(DropDownClosedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="LookupFailed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent LookupFailedEvent = EventManager.RegisterRoutedEvent(
            nameof(LookupFailed), RoutingStrategy.Bubble, typeof(EventHandler<AutoCompleteBoxLookupFailedEventArgs>), typeof(AutoCompleteBox));

        /// <summary>
        /// Occurs when the asynchronous item provider throws an exception.
        /// </summary>
        [Category("Behavior")]
        [Description("Raised when the asynchronous item provider throws an exception.")]
        public event EventHandler<AutoCompleteBoxLookupFailedEventArgs> LookupFailed
        {
            add => AddHandler(LookupFailedEvent, value);
            remove => RemoveHandler(LookupFailedEvent, value);
        }

        /// <summary>
        /// Gets the current filtered suggestions displayed by the dropdown.
        /// </summary>
        public ObservableCollection<object> FilteredItems { get; } = new();

        private static readonly DependencyPropertyKey HasNoResultsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasNoResults), typeof(bool), typeof(AutoCompleteBox), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="HasNoResults"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasNoResultsProperty = HasNoResultsPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether there are no filtered suggestions for the current search.
        /// </summary>
        public bool HasNoResults
        {
            get => (bool)GetValue(HasNoResultsProperty);
            private set => SetValue(HasNoResultsPropertyKey, value);
        }

        static AutoCompleteBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(AutoCompleteBox), new FrameworkPropertyMetadata(typeof(AutoCompleteBox)));
            FocusableProperty.OverrideMetadata(typeof(AutoCompleteBox), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoCompleteBox"/> class.
        /// </summary>
        public AutoCompleteBox()
        {
            _lookupTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = LookupDelay
            };
            _lookupTimer.Tick += LookupTimerOnTick;

            FilteredItems.CollectionChanged += FilteredItemsOnCollectionChanged;
            Unloaded += OnUnloaded;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            DetachTemplateParts();
            base.OnApplyTemplate();

            _textBox = GetTemplateChild(PartTextBox) as TextBox;
            _dropDownButton = GetTemplateChild(PartDropDownButton) as ButtonBase;
            _popup = GetTemplateChild(PartPopup) as Popup;
            _listBox = GetTemplateChild(PartListBox) as ListBox;

            if (_textBox != null)
            {
                _textBox.TextChanged += TextBoxOnTextChanged;
                _textBox.PreviewKeyDown += TextBoxOnPreviewKeyDown;
                _textBox.LostKeyboardFocus += TextBoxOnLostKeyboardFocus;
                _textBox.Text = Text;
            }

            if (_dropDownButton != null)
            {
                _dropDownButton.Click += DropDownButtonOnClick;
            }

            if (_popup != null)
            {
                _popup.Opened += PopupOnOpened;
                _popup.Closed += PopupOnClosed;
            }

            if (_listBox != null)
            {
                ConfigureListBoxItemDisplay();
                _listBox.SelectionChanged += ListBoxOnSelectionChanged;
                _listBox.PreviewMouseLeftButtonDown += ListBoxOnPreviewMouseLeftButtonDown;
            }

            RefreshSuggestions(false, false);
        }

        /// <summary>
        /// Opens the suggestion dropdown.
        /// </summary>
        public void OpenDropDown()
        {
            if (!IsEnabled)
            {
                return;
            }

            RefreshSuggestions(true);
            SetCurrentValue(IsDropDownOpenProperty, FilteredItems.Count > 0 || IsLoading || HasNoResults);
        }

        /// <summary>
        /// Closes the suggestion dropdown.
        /// </summary>
        public void CloseDropDown()
        {
            SetCurrentValue(IsDropDownOpenProperty, false);
        }

        /// <inheritdoc />
        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);

            if (_isCommittingSelection)
            {
                return;
            }

            if (SelectedItem != null)
            {
                UpdateTextFromSelection();
            }
            else if (!_isUpdatingText)
            {
                SetCurrentValue(TextProperty, string.Empty);
            }
        }

        /// <inheritdoc />
        protected override void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
        {
            base.OnItemsSourceChanged(oldValue, newValue);
            DetachItemsSourceCollectionChanged();
            AttachItemsSourceCollectionChanged(newValue);
            RefreshSuggestions(false, IsDropDownOpen);
        }

        /// <inheritdoc />
        protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
        {
            base.OnItemsChanged(e);
            if (_isCommittingSelection)
            {
                return;
            }

            RefreshSuggestions(false, IsDropDownOpen);
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.Property == DisplayMemberPathProperty || e.Property == ItemTemplateProperty || e.Property == ItemTemplateSelectorProperty)
            {
                ConfigureListBoxItemDisplay();
            }
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new AutoCompleteBoxAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
        {
            base.OnGotKeyboardFocus(e);
            _textBox?.Focus();
        }

        private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AutoCompleteBox autoCompleteBox)
            {
                return;
            }

            var oldValue = e.OldValue as string ?? string.Empty;
            var newValue = e.NewValue as string ?? string.Empty;
            autoCompleteBox.SyncTextBoxText(newValue);
            autoCompleteBox.RaiseAutomationValueChanged(oldValue, newValue);

            if (!autoCompleteBox._isUpdatingText && !autoCompleteBox._isCommittingSelection)
            {
                autoCompleteBox.ScheduleLookup();
            }
        }

        private static void OnIsDropDownOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not AutoCompleteBox autoCompleteBox)
            {
                return;
            }

            var oldValue = (bool)e.OldValue;
            var newValue = (bool)e.NewValue;

            if (newValue)
            {
                autoCompleteBox.RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, autoCompleteBox));
            }
            else
            {
                autoCompleteBox.RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, autoCompleteBox));
            }

            autoCompleteBox.RaiseAutomationExpandCollapseChanged(oldValue, newValue);
        }

        private static void OnSearchBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteBox autoCompleteBox)
            {
                autoCompleteBox.RefreshSuggestions(false, autoCompleteBox.IsDropDownOpen);
            }
        }

        private static void OnLookupDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is AutoCompleteBox autoCompleteBox)
            {
                autoCompleteBox._lookupTimer.Interval = (TimeSpan)e.NewValue;
                autoCompleteBox.ScheduleLookup();
            }
        }

        private static object CoerceNonNegativeInt(DependencyObject d, object baseValue)
        {
            return Math.Max(0, (int)baseValue);
        }

        private static object CoercePositiveInt(DependencyObject d, object baseValue)
        {
            return Math.Max(1, (int)baseValue);
        }

        private static object CoerceNonNegativeTimeSpan(DependencyObject d, object baseValue)
        {
            var value = (TimeSpan)baseValue;
            return value < TimeSpan.Zero ? TimeSpan.Zero : value;
        }

        private void TextBoxOnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_textBox == null || _isUpdatingText)
            {
                return;
            }

            SetCurrentValue(TextProperty, _textBox.Text);
        }

        private void TextBoxOnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Down when Keyboard.Modifiers.HasFlag(ModifierKeys.Alt):
                    OpenDropDown();
                    e.Handled = true;
                    break;
                case Key.F4:
                    if (IsDropDownOpen)
                    {
                        CloseDropDown();
                    }
                    else
                    {
                        OpenDropDown();
                    }

                    e.Handled = true;
                    break;
                case Key.Down:
                    MoveSelection(1);
                    e.Handled = true;
                    break;
                case Key.Up:
                    MoveSelection(-1);
                    e.Handled = true;
                    break;
                case Key.Enter:
                case Key.Tab:
                    if (IsDropDownOpen && CommitHighlightedItem())
                    {
                        e.Handled = e.Key == Key.Enter;
                    }
                    break;
                case Key.Escape:
                    RestoreCommittedText();
                    CloseDropDown();
                    e.Handled = true;
                    break;
            }
        }

        private void TextBoxOnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (IsKeyboardFocusWithin || IsDescendantFocus(e.NewFocus))
            {
                return;
            }

            RestoreCommittedText();
            CloseDropDown();
        }

        private void DropDownButtonOnClick(object sender, RoutedEventArgs e)
        {
            if (IsDropDownOpen)
            {
                CloseDropDown();
            }
            else
            {
                OpenDropDown();
                _textBox?.Focus();
            }
        }

        private void PopupOnOpened(object? sender, EventArgs e)
        {
            if (!IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, true);
            }
        }

        private void PopupOnClosed(object? sender, EventArgs e)
        {
            if (IsDropDownOpen)
            {
                SetCurrentValue(IsDropDownOpenProperty, false);
            }
        }

        private void ListBoxOnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_listBox?.SelectedItem != null)
            {
                _listBox.ScrollIntoView(_listBox.SelectedItem);
            }
        }

        private void ListBoxOnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not ListBoxItem itemContainer)
            {
                return;
            }

            _listBox!.SelectedItem = itemContainer.DataContext;
            CommitItem(itemContainer.DataContext);
            e.Handled = true;
        }

        private void LookupTimerOnTick(object? sender, EventArgs e)
        {
            _lookupTimer.Stop();
            RefreshSuggestions(false);
        }

        private void FilteredItemsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            NotifyHasNoResultsChanged();
        }

        private void ItemsSourceOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            RefreshSuggestions(false, IsDropDownOpen);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _lookupTimer.Stop();
            _lookupCancellationTokenSource?.Cancel();
            _lookupCancellationTokenSource?.Dispose();
            _lookupCancellationTokenSource = null;
            DetachTemplateParts();
            DetachItemsSourceCollectionChanged();
        }

        private void ScheduleLookup()
        {
            _lookupTimer.Stop();
            if (!CanQuerySuggestions(false))
            {
                ClearSuggestions();
                CloseDropDown();
                return;
            }

            _lookupTimer.Start();
        }

        private async void RefreshSuggestions(bool showAll)
        {
            RefreshSuggestions(showAll, true);
        }

        private async void RefreshSuggestions(bool showAll, bool openDropDown)
        {
            if (!IsEnabled)
            {
                ClearSuggestions();
                return;
            }

            var searchText = showAll ? string.Empty : Text;
            if (!CanQuerySuggestions(showAll))
            {
                ClearSuggestions();
                return;
            }

            LookupException = null;

            if (ItemsProvider != null)
            {
                await RefreshProviderSuggestionsAsync(searchText, openDropDown).ConfigureAwait(false);
                return;
            }

            RefreshLocalSuggestions(searchText, showAll, openDropDown);
        }

        private async Task RefreshProviderSuggestionsAsync(string searchText, bool openDropDown)
        {
            _lookupCancellationTokenSource?.Cancel();
            _lookupCancellationTokenSource?.Dispose();
            var cancellationTokenSource = new CancellationTokenSource();
            _lookupCancellationTokenSource = cancellationTokenSource;
            var version = ++_lookupVersion;
            IsLoading = true;
            NotifyHasNoResultsChanged();

            try
            {
                var provider = ItemsProvider;
                var results = provider == null ? null : await provider(searchText, cancellationTokenSource.Token).ConfigureAwait(false);
                await Dispatcher.InvokeAsync(() =>
                {
                    if (version != _lookupVersion || cancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    ReplaceSuggestions(ApplySuggestionLimit(results?.Cast<object>() ?? Enumerable.Empty<object>()));
                    IsLoading = false;
                    SetDropDownForSuggestionState(openDropDown || IsDropDownOpen);
                });
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (version == _lookupVersion)
                    {
                        IsLoading = false;
                        NotifyHasNoResultsChanged();
                    }
                });
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    if (version != _lookupVersion)
                    {
                        return;
                    }

                    LookupException = ex;
                    ClearSuggestions();
                    IsLoading = false;
                    RaiseEvent(new AutoCompleteBoxLookupFailedEventArgs(LookupFailedEvent, this, ex));
                    SetDropDownForSuggestionState(openDropDown || IsDropDownOpen);
                });
            }
        }

        private void RefreshLocalSuggestions(string searchText, bool showAll, bool openDropDown)
        {
            var matches = GetItems()
                .Where(item => showAll || MatchesFilter(item, searchText));

            ReplaceSuggestions(ApplySuggestionLimit(matches));
            SetDropDownForSuggestionState(openDropDown || IsDropDownOpen);
        }

        private bool CanQuerySuggestions(bool showAll)
        {
            return showAll || !IsTextRequiredForSuggestions || Text.Length >= MinimumPrefixLength;
        }

        private IEnumerable<object> GetItems()
        {
            foreach (var item in Items)
            {
                if (item != CollectionView.NewItemPlaceholder)
                {
                    yield return item;
                }
            }
        }

        private IEnumerable<object> ApplySuggestionLimit(IEnumerable<object> items)
        {
            return items.Take(MaxSuggestionCount).ToList();
        }

        private bool MatchesFilter(object item, string searchText)
        {
            if (FilterMode == AutoCompleteBoxFilterMode.Custom)
            {
                return FilterPredicate?.Invoke(item, searchText) ?? true;
            }

            var itemText = GetItemText(item);
            var comparison = CultureInfo.CurrentCulture.CompareInfo;
            return FilterMode switch
            {
                AutoCompleteBoxFilterMode.StartsWith => itemText.StartsWith(searchText, true, CultureInfo.CurrentCulture),
                _ => comparison.IndexOf(itemText, searchText, CompareOptions.IgnoreCase) >= 0
            };
        }

        private void ReplaceSuggestions(IEnumerable<object> items)
        {
            FilteredItems.Clear();
            foreach (var item in items)
            {
                FilteredItems.Add(item);
            }

            if (_listBox != null)
            {
                _listBox.SelectedIndex = FilteredItems.Count > 0 ? 0 : -1;
            }
        }

        private void ClearSuggestions()
        {
            FilteredItems.Clear();
            if (_listBox != null)
            {
                _listBox.SelectedIndex = -1;
            }

            NotifyHasNoResultsChanged();
        }

        private void MoveSelection(int offset)
        {
            if (!IsDropDownOpen)
            {
                OpenDropDown();
            }

            if (_listBox == null || FilteredItems.Count == 0)
            {
                return;
            }

            var nextIndex = _listBox.SelectedIndex < 0 ? 0 : _listBox.SelectedIndex + offset;
            nextIndex = Math.Clamp(nextIndex, 0, FilteredItems.Count - 1);
            _listBox.SelectedIndex = nextIndex;
            _listBox.ScrollIntoView(_listBox.SelectedItem);
        }

        private bool CommitHighlightedItem()
        {
            var item = _listBox?.SelectedItem;
            if (item == null)
            {
                return false;
            }

            CommitItem(item);
            return true;
        }

        private void CommitItem(object item)
        {
            try
            {
                _isCommittingSelection = true;
                EnsureItemCanBeSelected(item);
                SelectedItem = item;
                UpdateTextFromSelection();
            }
            finally
            {
                _isCommittingSelection = false;
            }

            CloseDropDown();
            _textBox?.Select(Text.Length, 0);
        }

        private void RestoreCommittedText()
        {
            if (SelectedItem == null)
            {
                SetCurrentValue(TextProperty, string.Empty);
                return;
            }

            UpdateTextFromSelection();
        }

        private void UpdateTextFromSelection()
        {
            if (SelectedItem == null)
            {
                return;
            }

            SetTextWithoutLookup(GetItemText(SelectedItem));
        }

        private void SetTextWithoutLookup(string text)
        {
            try
            {
                _isUpdatingText = true;
                SetCurrentValue(TextProperty, text);
                SyncTextBoxText(text);
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private void SyncTextBoxText(string text)
        {
            if (_textBox == null || _textBox.Text == text)
            {
                return;
            }

            try
            {
                _isUpdatingText = true;
                _textBox.Text = text;
                _textBox.CaretIndex = text.Length;
            }
            finally
            {
                _isUpdatingText = false;
            }
        }

        private string GetItemText(object item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            var textPath = TextSearch.GetTextPath(this);
            if (!string.IsNullOrWhiteSpace(textPath) && TryGetPropertyPathValue(item, textPath, out var textPathValue))
            {
                return Convert.ToString(textPathValue, CultureInfo.CurrentCulture) ?? string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(DisplayMemberPath) && TryGetPropertyPathValue(item, DisplayMemberPath, out var displayValue))
            {
                return Convert.ToString(displayValue, CultureInfo.CurrentCulture) ?? string.Empty;
            }

            return item.ToString() ?? string.Empty;
        }

        private static bool TryGetPropertyPathValue(object item, string propertyPath, out object? value)
        {
            value = item;
            foreach (var propertyName in propertyPath.Split('.'))
            {
                if (value == null)
                {
                    return false;
                }

                var property = TypeDescriptor.GetProperties(value)[propertyName];
                if (property == null)
                {
                    return false;
                }

                value = property.GetValue(value);
            }

            return true;
        }

        private void SetDropDownForSuggestionState(bool openDropDown)
        {
            NotifyHasNoResultsChanged();
            if (openDropDown)
            {
                SetCurrentValue(IsDropDownOpenProperty, FilteredItems.Count > 0 || IsLoading || HasNoResults);
            }
        }

        private void EnsureItemCanBeSelected(object item)
        {
            if (ItemsSource != null || Items.Contains(item))
            {
                return;
            }

            Items.Add(item);
        }

        private void NotifyHasNoResultsChanged()
        {
            HasNoResults = !IsLoading && Text.Length >= MinimumPrefixLength && FilteredItems.Count == 0;
        }

        private bool IsDescendantFocus(object? focus)
        {
            if (focus is not DependencyObject dependencyObject)
            {
                return false;
            }

            DependencyObject? current = dependencyObject;
            while (current != null)
            {
                if (ReferenceEquals(current, this))
                {
                    return true;
                }

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        private void AttachItemsSourceCollectionChanged(IEnumerable? itemsSource)
        {
            if (itemsSource is INotifyCollectionChanged collectionChanged)
            {
                _itemsSourceCollectionChanged = collectionChanged;
                CollectionChangedEventManager.AddHandler(collectionChanged, ItemsSourceOnCollectionChanged);
            }
        }

        private void DetachItemsSourceCollectionChanged()
        {
            if (_itemsSourceCollectionChanged != null)
            {
                CollectionChangedEventManager.RemoveHandler(_itemsSourceCollectionChanged, ItemsSourceOnCollectionChanged);
                _itemsSourceCollectionChanged = null;
            }
        }

        private void DetachTemplateParts()
        {
            if (_textBox != null)
            {
                _textBox.TextChanged -= TextBoxOnTextChanged;
                _textBox.PreviewKeyDown -= TextBoxOnPreviewKeyDown;
                _textBox.LostKeyboardFocus -= TextBoxOnLostKeyboardFocus;
                _textBox = null;
            }

            if (_dropDownButton != null)
            {
                _dropDownButton.Click -= DropDownButtonOnClick;
                _dropDownButton = null;
            }

            if (_popup != null)
            {
                _popup.Opened -= PopupOnOpened;
                _popup.Closed -= PopupOnClosed;
                _popup = null;
            }

            if (_listBox != null)
            {
                _listBox.SelectionChanged -= ListBoxOnSelectionChanged;
                _listBox.PreviewMouseLeftButtonDown -= ListBoxOnPreviewMouseLeftButtonDown;
                _listBox = null;
            }
        }

        private void ConfigureListBoxItemDisplay()
        {
            if (_listBox == null)
            {
                return;
            }

            _listBox.ClearValue(DisplayMemberPathProperty);
            _listBox.ClearValue(ItemTemplateProperty);
            _listBox.ClearValue(ItemTemplateSelectorProperty);

            if (ItemTemplate != null)
            {
                _listBox.ItemTemplate = ItemTemplate;
                return;
            }

            if (ItemTemplateSelector != null)
            {
                _listBox.ItemTemplateSelector = ItemTemplateSelector;
                return;
            }

            if (!string.IsNullOrWhiteSpace(DisplayMemberPath))
            {
                _listBox.DisplayMemberPath = DisplayMemberPath;
            }
        }

        private static T? FindAncestor<T>(DependencyObject? source) where T : DependencyObject
        {
            var current = source;
            while (current != null)
            {
                if (current is T match)
                {
                    return match;
                }

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }

        private void RaiseAutomationExpandCollapseChanged(bool oldValue, bool newValue)
        {
            if (UIElementAutomationPeer.FromElement(this) is AutoCompleteBoxAutomationPeer peer)
            {
                peer.RaiseExpandCollapseStateChanged(oldValue, newValue);
            }
        }

        private void RaiseAutomationValueChanged(string oldValue, string newValue)
        {
            if (UIElementAutomationPeer.FromElement(this) is AutoCompleteBoxAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldValue, newValue);
            }
        }
    }
}
