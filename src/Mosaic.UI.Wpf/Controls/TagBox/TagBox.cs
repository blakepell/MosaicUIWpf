/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

using CommunityToolkit.Mvvm.Input;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A specialized input control that turns typed text into removable, vibrantly-colored "tags". Pressing
    /// <see cref="Key.Enter"/> commits the current text as a new tag, each tag carries an "✕" button to remove it, and
    /// pressing <see cref="Key.Back"/> while the caret sits just to the right of the tags removes the last one. Tags are
    /// surfaced through the bindable <see cref="Tags"/> collection, and the <see cref="TagChanging"/> /
    /// <see cref="TagChanged"/> events allow callers to veto or observe every change.
    /// <para>
    /// When a <see cref="SuggestionsSource"/> is supplied the control additionally behaves like an auto-complete box:
    /// typing filters the source and shows a drop-down of candidate tags that can be committed with the mouse, with
    /// <see cref="Key.Tab"/>, or with <see cref="Key.Enter"/> once one has been arrowed to. Free-form entry is always
    /// still allowed. <see cref="Key.Escape"/> discards the pending text without disturbing the committed tags, and
    /// <see cref="Key.Tab"/> falls through to normal focus navigation when there is nothing pending to commit.
    /// </para>
    /// </summary>
    [TemplatePart(Name = PartTextBox, Type = typeof(TextBox))]
    [TemplatePart(Name = PartTagPanel, Type = typeof(Panel))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartSuggestionList, Type = typeof(ListBox))]
    [DefaultProperty(nameof(Tags))]
    [DefaultEvent(nameof(TagChanged))]
    public class TagBox : Control
    {
        private const string PartTextBox = "PART_TextBox";
        private const string PartTagPanel = "PART_TagPanel";
        private const string PartPopup = "PART_Popup";
        private const string PartSuggestionList = "PART_SuggestionList";

        /// <summary>
        /// The text entry portion of the control where new tags are typed.
        /// </summary>
        private TextBox? _textBox;

        /// <summary>
        /// The panel that hosts the tag chips followed by the input <see cref="_textBox"/>.
        /// </summary>
        private Panel? _tagPanel;

        /// <summary>
        /// The popup that hosts the auto-complete suggestion list.
        /// </summary>
        private Popup? _popup;

        /// <summary>
        /// The list that displays <see cref="FilteredSuggestions"/>.
        /// </summary>
        private ListBox? _suggestionList;

        /// <summary>
        /// Tracks the <see cref="SuggestionsSource"/> instance we are subscribed to for change notifications.
        /// </summary>
        private INotifyCollectionChanged? _suggestionsCollectionChanged;

        /// <summary>
        /// Set while the control is writing to the input box itself so that the resulting <c>TextChanged</c> does not
        /// re-trigger a suggestion lookup.
        /// </summary>
        private bool _isUpdatingText;

        /// <summary>
        /// Set once the user has arrowed into the drop-down. Until then <see cref="Key.Enter"/> commits the typed text
        /// verbatim so that free-form tags are never silently replaced by the top match.
        /// </summary>
        private bool _isSuggestionHighlighted;

        /// <summary>
        /// Debounces the suggestion lookup while the user is typing.
        /// </summary>
        private readonly DispatcherTimer _suggestionTimer;

        /// <summary>
        /// Whether the debounced lookup that is currently pending is allowed to open the drop-down.
        /// </summary>
        private bool _pendingLookupOpensDropDown;

        /// <summary>
        /// Raised after a tag has been added to or removed from the <see cref="Tags"/> collection.
        /// </summary>
        public event EventHandler<TagChangedEventArgs>? TagChanged;

        /// <summary>
        /// Raised before a tag is added to or removed from the <see cref="Tags"/> collection. Set
        /// <see cref="System.ComponentModel.CancelEventArgs.Cancel"/> to <see langword="true"/> to veto the change.
        /// </summary>
        public event EventHandler<TagChangingEventArgs>? TagChanging;

        /// <summary>
        /// The command used by a tag chip's delete button to remove that tag.
        /// </summary>
        public ICommand DeleteTagCommand { get; }

        /// <summary>
        /// Identifies the <see cref="Tags"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagsProperty = DependencyProperty.Register(
            nameof(Tags), typeof(ObservableCollection<string>), typeof(TagBox),
            new FrameworkPropertyMetadata(null, OnTagsChanged));

        /// <summary>
        /// Gets or sets the collection of tags. Adding or removing items from this collection (whether through the UI
        /// or directly in code) raises <see cref="TagChanged"/> and keeps the displayed chips in sync.
        /// </summary>
        [Category("Common")]
        [Description("The collection of tags displayed by the control.")]
        public ObservableCollection<string> Tags
        {
            get => (ObservableCollection<string>)GetValue(TagsProperty);
            set => SetValue(TagsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Watermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty WatermarkProperty = DependencyProperty.Register(
            nameof(Watermark), typeof(string), typeof(TagBox), new PropertyMetadata("Add a tag..."));

        /// <summary>
        /// Gets or sets the placeholder text shown when there are no tags and no text has been entered.
        /// </summary>
        [Category("Common")]
        [Description("The placeholder text shown when the control is empty.")]
        public string Watermark
        {
            get => (string)GetValue(WatermarkProperty);
            set => SetValue(WatermarkProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowDuplicates"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowDuplicatesProperty = DependencyProperty.Register(
            nameof(AllowDuplicates), typeof(bool), typeof(TagBox), new PropertyMetadata(false));

        /// <summary>
        /// Gets or sets a value indicating whether duplicate tags are allowed. When <see langword="false"/> (the
        /// default), duplicates are rejected using a case-insensitive comparison.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether duplicate tags are allowed.")]
        public bool AllowDuplicates
        {
            get => (bool)GetValue(AllowDuplicatesProperty);
            set => SetValue(AllowDuplicatesProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagBackgroundProperty = DependencyProperty.Register(
            nameof(TagBackground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the fill brush used for every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The fill brush used for every tag chip.")]
        public Brush? TagBackground
        {
            get => (Brush?)GetValue(TagBackgroundProperty);
            set => SetValue(TagBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagForegroundProperty = DependencyProperty.Register(
            nameof(TagForeground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the text/glyph brush used inside every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The text and delete-glyph brush used inside every tag chip.")]
        public Brush? TagForeground
        {
            get => (Brush?)GetValue(TagForegroundProperty);
            set => SetValue(TagForegroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagBorderBrushProperty = DependencyProperty.Register(
            nameof(TagBorderBrush), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the border brush used for every tag chip.
        /// </summary>
        [Category("Brushes")]
        [Description("The border brush used for every tag chip.")]
        public Brush? TagBorderBrush
        {
            get => (Brush?)GetValue(TagBorderBrushProperty);
            set => SetValue(TagBorderBrushProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagDeleteHoverBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagDeleteHoverBackgroundProperty = DependencyProperty.Register(
            nameof(TagDeleteHoverBackground), typeof(Brush), typeof(TagBox), new PropertyMetadata(null));

        /// <summary>
        /// Gets or sets the background brush shown behind a chip's delete glyph while the pointer is over it.
        /// </summary>
        [Category("Brushes")]
        [Description("The background brush shown behind a chip's delete glyph on hover.")]
        public Brush? TagDeleteHoverBackground
        {
            get => (Brush?)GetValue(TagDeleteHoverBackgroundProperty);
            set => SetValue(TagDeleteHoverBackgroundProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="TagCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TagCornerRadiusProperty = DependencyProperty.Register(
            nameof(TagCornerRadius), typeof(CornerRadius), typeof(TagBox), new PropertyMetadata(new CornerRadius(10)));

        /// <summary>
        /// Gets or sets the corner radius applied to each tag chip. Defaults to a rounded "pill" shape.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius applied to each tag chip.")]
        public CornerRadius TagCornerRadius
        {
            get => (CornerRadius)GetValue(TagCornerRadiusProperty);
            set => SetValue(TagCornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(TagBox), new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets or sets the corner radius applied to each tag chip. Defaults to a rounded "pill" shape.
        /// </summary>
        [Category("Appearance")]
        [Description("The corner radius applied the tag box itself.")]
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SuggestionsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionsSourceProperty = DependencyProperty.Register(
            nameof(SuggestionsSource), typeof(IEnumerable), typeof(TagBox),
            new FrameworkPropertyMetadata(null, OnSuggestionsSourceChanged));

        /// <summary>
        /// Gets or sets the pool of candidate tags used for auto-complete. Typically an
        /// <see cref="ObservableCollection{T}"/> of <see cref="string"/>; any enumerable works and each item's
        /// <see cref="object.ToString"/> value supplies the tag text. When <see langword="null"/> the control behaves
        /// exactly as a plain tag editor with no drop-down.
        /// </summary>
        [Category("Common")]
        [Description("The pool of candidate tags used for auto-complete suggestions.")]
        public IEnumerable? SuggestionsSource
        {
            get => (IEnumerable?)GetValue(SuggestionsSourceProperty);
            set => SetValue(SuggestionsSourceProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsSuggestionListOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSuggestionListOpenProperty = DependencyProperty.Register(
            nameof(IsSuggestionListOpen), typeof(bool), typeof(TagBox),
            new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSuggestionListOpenChanged));

        /// <summary>
        /// Gets or sets a value indicating whether the auto-complete drop-down is open.
        /// </summary>
        [Category("Common")]
        [Description("Indicates whether the auto-complete drop-down is open.")]
        public bool IsSuggestionListOpen
        {
            get => (bool)GetValue(IsSuggestionListOpenProperty);
            set => SetValue(IsSuggestionListOpenProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinimumPrefixLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumPrefixLengthProperty = DependencyProperty.Register(
            nameof(MinimumPrefixLength), typeof(int), typeof(TagBox),
            new PropertyMetadata(1, OnSuggestionBehaviorChanged, CoerceNonNegativeInt));

        /// <summary>
        /// Gets or sets the minimum number of typed characters required before suggestions are shown. A value of zero
        /// shows the full (unfiltered) list as soon as the control receives focus.
        /// </summary>
        [Category("Behavior")]
        [Description("Minimum number of typed characters required before suggestions are shown.")]
        public int MinimumPrefixLength
        {
            get => (int)GetValue(MinimumPrefixLengthProperty);
            set => SetValue(MinimumPrefixLengthProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaxSuggestionCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaxSuggestionCountProperty = DependencyProperty.Register(
            nameof(MaxSuggestionCount), typeof(int), typeof(TagBox),
            new PropertyMetadata(25, OnSuggestionBehaviorChanged, CoercePositiveInt));

        /// <summary>
        /// Gets or sets the maximum number of suggestions displayed in the drop-down.
        /// </summary>
        [Category("Behavior")]
        [Description("Maximum number of suggestions displayed in the drop-down.")]
        public int MaxSuggestionCount
        {
            get => (int)GetValue(MaxSuggestionCountProperty);
            set => SetValue(MaxSuggestionCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SuggestionFilterMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionFilterModeProperty = DependencyProperty.Register(
            nameof(SuggestionFilterMode), typeof(AutoCompleteBoxFilterMode), typeof(TagBox),
            new PropertyMetadata(AutoCompleteBoxFilterMode.Contains, OnSuggestionBehaviorChanged));

        /// <summary>
        /// Gets or sets how the typed text is matched against <see cref="SuggestionsSource"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("How the typed text is matched against the suggestion source.")]
        public AutoCompleteBoxFilterMode SuggestionFilterMode
        {
            get => (AutoCompleteBoxFilterMode)GetValue(SuggestionFilterModeProperty);
            set => SetValue(SuggestionFilterModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SuggestionFilterPredicate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionFilterPredicateProperty = DependencyProperty.Register(
            nameof(SuggestionFilterPredicate), typeof(AutoCompleteItemFilter), typeof(TagBox),
            new PropertyMetadata(null, OnSuggestionBehaviorChanged));

        /// <summary>
        /// Gets or sets the custom filter used when <see cref="SuggestionFilterMode"/> is
        /// <see cref="AutoCompleteBoxFilterMode.Custom"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Custom suggestion filter used when SuggestionFilterMode is Custom.")]
        public AutoCompleteItemFilter? SuggestionFilterPredicate
        {
            get => (AutoCompleteItemFilter?)GetValue(SuggestionFilterPredicateProperty);
            set => SetValue(SuggestionFilterPredicateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ExcludeExistingTagsFromSuggestions"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExcludeExistingTagsFromSuggestionsProperty = DependencyProperty.Register(
            nameof(ExcludeExistingTagsFromSuggestions), typeof(bool), typeof(TagBox),
            new PropertyMetadata(true, OnSuggestionBehaviorChanged));

        /// <summary>
        /// Gets or sets a value indicating whether tags that have already been added are hidden from the drop-down.
        /// Defaults to <see langword="true"/>.
        /// </summary>
        [Category("Behavior")]
        [Description("Whether tags that have already been added are hidden from the drop-down.")]
        public bool ExcludeExistingTagsFromSuggestions
        {
            get => (bool)GetValue(ExcludeExistingTagsFromSuggestionsProperty);
            set => SetValue(ExcludeExistingTagsFromSuggestionsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SuggestionDelay"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionDelayProperty = DependencyProperty.Register(
            nameof(SuggestionDelay), typeof(TimeSpan), typeof(TagBox),
            new PropertyMetadata(TimeSpan.FromMilliseconds(100), OnSuggestionDelayChanged, CoerceNonNegativeTimeSpan));

        /// <summary>
        /// Gets or sets how long typing pauses before the drop-down is refreshed. A short debounce keeps the list from
        /// flickering on every keystroke. Set to <see cref="TimeSpan.Zero"/> to filter synchronously.
        /// </summary>
        [Category("Behavior")]
        [Description("How long typing pauses before the suggestion drop-down is refreshed.")]
        public TimeSpan SuggestionDelay
        {
            get => (TimeSpan)GetValue(SuggestionDelayProperty);
            set => SetValue(SuggestionDelayProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SuggestionListMaxHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SuggestionListMaxHeightProperty = DependencyProperty.Register(
            nameof(SuggestionListMaxHeight), typeof(double), typeof(TagBox), new PropertyMetadata(220.0));

        /// <summary>
        /// Gets or sets the maximum height of the auto-complete drop-down.
        /// </summary>
        [Category("Layout")]
        [Description("Maximum height of the auto-complete drop-down.")]
        public double SuggestionListMaxHeight
        {
            get => (double)GetValue(SuggestionListMaxHeightProperty);
            set => SetValue(SuggestionListMaxHeightProperty, value);
        }

        /// <summary>
        /// Gets the suggestions currently displayed by the drop-down for the typed text.
        /// </summary>
        [Browsable(false)]
        public ObservableCollection<string> FilteredSuggestions { get; } = new();

        private static readonly DependencyPropertyKey ShowWatermarkPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ShowWatermark), typeof(bool), typeof(TagBox), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="ShowWatermark"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowWatermarkProperty = ShowWatermarkPropertyKey.DependencyProperty;

        /// <summary>
        /// Gets a value indicating whether the watermark should currently be displayed (used by the control template).
        /// </summary>
        [Browsable(false)]
        public bool ShowWatermark
        {
            get => (bool)GetValue(ShowWatermarkProperty);
            private set => SetValue(ShowWatermarkPropertyKey, value);
        }

        /// <summary>
        /// Initializes static members of the <see cref="TagBox"/> class.
        /// </summary>
        static TagBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TagBox), new FrameworkPropertyMetadata(typeof(TagBox)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TagBox"/> class.
        /// </summary>
        public TagBox()
        {
            this.DeleteTagCommand = new RelayCommand<string>(tag => this.RemoveTag(tag));
            this.SetCurrentValue(TagsProperty, new ObservableCollection<string>());

            _suggestionTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = this.SuggestionDelay
            };

            _suggestionTimer.Tick += SuggestionTimerOnTick;
            this.Unloaded += (_, _) => _suggestionTimer.Stop();
        }

        /// <summary>
        /// Runs the lookup that was debounced by the last keystroke.
        /// </summary>
        private void SuggestionTimerOnTick(object? sender, EventArgs e)
        {
            _suggestionTimer.Stop();
            this.RefreshSuggestions(_pendingLookupOpensDropDown);
        }

        /// <summary>
        /// Queues a suggestion refresh, waiting out <see cref="SuggestionDelay"/> so that a burst of keystrokes only
        /// filters once. A zero delay refreshes synchronously.
        /// </summary>
        /// <param name="openDropDown">Whether the refresh may open the drop-down once it runs.</param>
        private void ScheduleSuggestionRefresh(bool openDropDown)
        {
            _suggestionTimer.Stop();

            if (this.SuggestionDelay <= TimeSpan.Zero)
            {
                this.RefreshSuggestions(openDropDown);
                return;
            }

            _pendingLookupOpensDropDown = openDropDown;
            _suggestionTimer.Start();
        }

        /// <summary>
        /// Runs any debounced lookup immediately so that a commit never acts on a stale drop-down.
        /// </summary>
        private void FlushPendingSuggestionRefresh()
        {
            if (_suggestionTimer.IsEnabled)
            {
                _suggestionTimer.Stop();
                this.RefreshSuggestions(_pendingLookupOpensDropDown);
            }
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.DetachTemplateParts();

            _tagPanel = this.GetTemplateChild(PartTagPanel) as Panel;
            _textBox = this.GetTemplateChild(PartTextBox) as TextBox;
            _popup = this.GetTemplateChild(PartPopup) as Popup;
            _suggestionList = this.GetTemplateChild(PartSuggestionList) as ListBox;

            if (_textBox != null)
            {
                _textBox.PreviewKeyDown += TextBox_PreviewKeyDown;
                _textBox.TextChanged += TextBox_TextChanged;
                _textBox.LostKeyboardFocus += TextBox_LostKeyboardFocus;
            }

            if (_suggestionList != null)
            {
                _suggestionList.PreviewMouseLeftButtonDown += SuggestionList_PreviewMouseLeftButtonDown;
            }

            this.RebuildTags();
            this.UpdateWatermark();
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TagBoxAutomationPeer(this);
        }

        /// <summary>
        /// Unhooks any handlers attached to the previous template's parts.
        /// </summary>
        private void DetachTemplateParts()
        {
            if (_textBox != null)
            {
                _textBox.PreviewKeyDown -= TextBox_PreviewKeyDown;
                _textBox.TextChanged -= TextBox_TextChanged;
                _textBox.LostKeyboardFocus -= TextBox_LostKeyboardFocus;
                _textBox = null;
            }

            if (_suggestionList != null)
            {
                _suggestionList.PreviewMouseLeftButtonDown -= SuggestionList_PreviewMouseLeftButtonDown;
                _suggestionList = null;
            }

            _popup = null;
            _tagPanel = null;
        }

        /// <inheritdoc />
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);

            // Clicking anywhere within the control (empty space, between chips, etc.) puts focus on the input box.
            if (_textBox != null && !_textBox.IsKeyboardFocusWithin)
            {
                _textBox.Focus();
                _textBox.CaretIndex = _textBox.Text.Length;
            }
        }

        /// <summary>
        /// Attempts to add a tag, raising <see cref="TagChanging"/> (which may cancel the operation) followed by
        /// <see cref="TagChanged"/>.
        /// </summary>
        /// <param name="tag">The tag text to add. Surrounding whitespace is trimmed.</param>
        /// <returns><see langword="true"/> if the tag was added; otherwise <see langword="false"/>.</returns>
        public bool AddTag(string? tag)
        {
            tag = tag?.Trim();

            if (string.IsNullOrEmpty(tag) || this.Tags == null)
            {
                return false;
            }

            if (!this.AllowDuplicates && this.ContainsTag(tag))
            {
                return false;
            }

            var changing = new TagChangingEventArgs(tag, TagChangeAction.Add);
            this.TagChanging?.Invoke(this, changing);

            if (changing.Cancel)
            {
                return false;
            }

            // The collection-changed handler raises TagChanged and refreshes the chips.
            this.Tags.Add(tag);
            return true;
        }

        /// <summary>
        /// Attempts to remove a tag, raising <see cref="TagChanging"/> (which may cancel the operation) followed by
        /// <see cref="TagChanged"/>.
        /// </summary>
        /// <param name="tag">The tag to remove.</param>
        /// <returns><see langword="true"/> if the tag was removed; otherwise <see langword="false"/>.</returns>
        public bool RemoveTag(string? tag)
        {
            if (tag == null || this.Tags == null || !this.Tags.Contains(tag))
            {
                return false;
            }

            var changing = new TagChangingEventArgs(tag, TagChangeAction.Remove);
            this.TagChanging?.Invoke(this, changing);

            if (changing.Cancel)
            {
                return false;
            }

            this.Tags.Remove(tag);
            return true;
        }

        /// <summary>
        /// Determines whether the specified tag already exists, using a case-insensitive comparison.
        /// </summary>
        /// <param name="tag">The candidate tag.</param>
        private bool ContainsTag(string tag)
        {
            return this.Tags?.Any(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase)) == true;
        }

        /// <summary>
        /// Handles the addition or removal of the <see cref="Tags"/> collection instance, rewiring change notifications.
        /// </summary>
        private static void OnTagsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TagBox)d;

            if (e.OldValue is ObservableCollection<string> oldCollection)
            {
                oldCollection.CollectionChanged -= control.OnTagsCollectionChanged;
            }

            if (e.NewValue is ObservableCollection<string> newCollection)
            {
                newCollection.CollectionChanged += control.OnTagsCollectionChanged;
            }

            control.RebuildTags();
            control.UpdateWatermark();
        }

        /// <summary>
        /// Keeps the displayed chips in sync with the backing collection and raises <see cref="TagChanged"/> for each
        /// added or removed item.
        /// </summary>
        private void OnTagsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    this.RaiseTagChanged(e.NewItems, TagChangeAction.Add);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    this.RaiseTagChanged(e.OldItems, TagChangeAction.Remove);
                    break;
                case NotifyCollectionChangedAction.Replace:
                    this.RaiseTagChanged(e.OldItems, TagChangeAction.Remove);
                    this.RaiseTagChanged(e.NewItems, TagChangeAction.Add);
                    break;
            }

            this.RebuildTags();
            this.UpdateWatermark();

            // The set of already-applied tags feeds the suggestion filter, so it has to be re-evaluated.
            if (this.ExcludeExistingTagsFromSuggestions)
            {
                this.RefreshSuggestions(this.IsSuggestionListOpen);
            }
        }

        /// <summary>
        /// Raises <see cref="TagChanged"/> for each item in the supplied list.
        /// </summary>
        private void RaiseTagChanged(System.Collections.IList? items, TagChangeAction action)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                if (item is string tag)
                {
                    this.TagChanged?.Invoke(this, new TagChangedEventArgs(tag, action));
                }
            }
        }

        /// <summary>
        /// Rebuilds the visual chips, leaving the input <see cref="_textBox"/> in place as the final child so that
        /// keyboard focus and caret position are preserved.
        /// </summary>
        private void RebuildTags()
        {
            if (_tagPanel == null || _textBox == null)
            {
                return;
            }

            for (int i = _tagPanel.Children.Count - 1; i >= 0; i--)
            {
                if (!ReferenceEquals(_tagPanel.Children[i], _textBox))
                {
                    _tagPanel.Children.RemoveAt(i);
                }
            }

            if (this.Tags == null)
            {
                return;
            }

            int index = 0;

            foreach (var tag in this.Tags)
            {
                _tagPanel.Children.Insert(index++, this.CreateChip(tag));
            }
        }

        /// <summary>
        /// Creates a single tag chip (a rounded border containing the tag text and a delete glyph). Colors are bound to
        /// the control's tag brush properties so they track theme and runtime changes.
        /// </summary>
        /// <param name="tag">The tag text the chip represents.</param>
        private UIElement CreateChip(string tag)
        {
            var chip = new Border
            {
                Margin = new Thickness(0, 2, 5, 2),
                Padding = new Thickness(9, 2, 4, 2),
                BorderThickness = new Thickness(1),
                SnapsToDevicePixels = true,
                VerticalAlignment = VerticalAlignment.Center
            };

            chip.SetBinding(Border.BackgroundProperty, this.SelfBinding(nameof(TagBackground)));
            chip.SetBinding(Border.BorderBrushProperty, this.SelfBinding(nameof(TagBorderBrush)));
            chip.SetBinding(Border.CornerRadiusProperty, this.SelfBinding(nameof(TagCornerRadius)));

            var panel = new StackPanel { Orientation = Orientation.Horizontal };

            var text = new TextBlock
            {
                Text = tag,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = FontWeights.Medium
            };
            text.SetBinding(TextBlock.ForegroundProperty, this.SelfBinding(nameof(TagForeground)));

            var deleteHost = new Border
            {
                Width = 16,
                Height = 16,
                Margin = new Thickness(5, 0, 0, 0),
                CornerRadius = new CornerRadius(8),
                Background = Brushes.Transparent,
                Cursor = Cursors.Hand,
                VerticalAlignment = VerticalAlignment.Center,
                ToolTip = $"Remove \"{tag}\""
            };

            var glyph = new TextBlock
            {
                Text = "✕",
                FontSize = 9,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            glyph.SetBinding(TextBlock.ForegroundProperty, this.SelfBinding(nameof(TagForeground)));
            deleteHost.Child = glyph;

            deleteHost.MouseEnter += (_, _) => deleteHost.SetBinding(Border.BackgroundProperty, this.SelfBinding(nameof(TagDeleteHoverBackground)));
            deleteHost.MouseLeave += (_, _) =>
            {
                System.Windows.Data.BindingOperations.ClearBinding(deleteHost, Border.BackgroundProperty);
                deleteHost.Background = Brushes.Transparent;
            };
            deleteHost.MouseLeftButtonUp += (_, e) =>
            {
                e.Handled = true;
                this.RemoveTag(tag);
            };

            panel.Children.Add(text);
            panel.Children.Add(deleteHost);
            chip.Child = panel;

            return chip;
        }

        /// <summary>
        /// Creates a one-way binding to a property on this control instance.
        /// </summary>
        private System.Windows.Data.Binding SelfBinding(string path)
        {
            return new System.Windows.Data.Binding(path) { Source = this };
        }

        /// <summary>
        /// Updates <see cref="ShowWatermark"/> based on whether there are any tags or pending input text.
        /// </summary>
        private void UpdateWatermark()
        {
            this.ShowWatermark = (this.Tags?.Count ?? 0) == 0 && string.IsNullOrEmpty(_textBox?.Text);
        }

        /// <summary>
        /// Handles Enter (commit current text as a tag) and Backspace (remove the last tag when the caret is at the
        /// start of an empty input, i.e. just to the right of the tags).
        /// </summary>
        private void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_textBox == null)
            {
                return;
            }

            if (e.Key is Key.Enter or Key.Tab)
            {
                // A fast typist can hit these before the debounce fires; commit against current text, not stale results.
                this.FlushPendingSuggestionRefresh();
            }

            switch (e.Key)
            {
                case Key.Enter:
                    // Only an explicitly arrowed-to suggestion wins over the raw text; otherwise free-form entry
                    // would be silently replaced by whatever happens to be at the top of the list.
                    var suggestion = this.IsSuggestionListOpen && _isSuggestionHighlighted
                        ? _suggestionList?.SelectedItem as string
                        : null;

                    if (this.AddTag(suggestion ?? _textBox.Text))
                    {
                        this.ClearInputText();
                    }

                    this.CloseSuggestionList();
                    e.Handled = true;
                    break;

                case Key.Tab:
                    // Tab commits whatever is pending: the selected suggestion if the drop-down is showing one,
                    // otherwise the typed text. With nothing pending it is left unhandled so focus moves on to the
                    // next tab stop.
                    string? pending = (this.IsSuggestionListOpen ? _suggestionList?.SelectedItem as string : null)
                                      ?? _textBox.Text;

                    if (string.IsNullOrWhiteSpace(pending))
                    {
                        break;
                    }

                    if (this.AddTag(pending))
                    {
                        this.ClearInputText();
                    }

                    this.CloseSuggestionList();
                    e.Handled = true;
                    break;

                case Key.Down:
                    if (!this.IsSuggestionListOpen)
                    {
                        this.OpenSuggestionList();

                        // Down deliberately steps into the list, so the first match counts as highlighted.
                        _isSuggestionHighlighted = this.IsSuggestionListOpen;
                    }
                    else
                    {
                        this.MoveSuggestionSelection(1);
                    }

                    e.Handled = true;
                    break;

                case Key.Up:
                    if (this.IsSuggestionListOpen)
                    {
                        this.MoveSuggestionSelection(-1);
                        e.Handled = true;
                    }

                    break;

                case Key.Escape:
                    // Escape discards the text that has not become a tag yet. Committed tags are left alone.
                    if (_textBox.Text.Length > 0 || this.IsSuggestionListOpen)
                    {
                        this.ClearInputText();
                        this.CloseSuggestionList();
                        e.Handled = true;
                    }

                    break;

                case Key.Back:
                    if (_textBox.CaretIndex == 0 && _textBox.SelectionLength == 0 && this.Tags is { Count: > 0 })
                    {
                        this.RemoveTag(this.Tags[^1]);
                        e.Handled = true;
                    }

                    break;
            }
        }

        /// <summary>
        /// Keeps the watermark state current as the user types and refreshes the auto-complete drop-down.
        /// </summary>
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            this.UpdateWatermark();

            if (!_isUpdatingText)
            {
                this.ScheduleSuggestionRefresh(this.IsKeyboardFocusWithin);
            }
        }

        /// <summary>
        /// Closes the drop-down when focus leaves the control entirely (but not when it moves to the popup).
        /// </summary>
        private void TextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (this.IsKeyboardFocusWithin || IsDescendantFocus(this, e.NewFocus))
            {
                return;
            }

            this.CloseSuggestionList();
        }

        /// <summary>
        /// Commits a suggestion clicked with the mouse.
        /// </summary>
        private void SuggestionList_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (FindAncestor<ListBoxItem>(e.OriginalSource as DependencyObject) is not { DataContext: string tag })
            {
                return;
            }

            e.Handled = true;

            if (this.AddTag(tag))
            {
                this.ClearInputText();
            }

            this.CloseSuggestionList();
            _textBox?.Focus();
        }

        /// <summary>
        /// Opens the auto-complete drop-down, showing every suggestion that matches the current input.
        /// </summary>
        public void OpenSuggestionList()
        {
            if (!this.IsEnabled)
            {
                return;
            }

            this.RefreshSuggestions(true);
        }

        /// <summary>
        /// Closes the auto-complete drop-down.
        /// </summary>
        public void CloseSuggestionList()
        {
            this.SetCurrentValue(IsSuggestionListOpenProperty, false);
        }

        /// <summary>
        /// Replaces the input text without triggering a suggestion lookup, leaving the caret at the end.
        /// </summary>
        private void SetInputText(string text)
        {
            if (_textBox == null)
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

            this.UpdateWatermark();
        }

        /// <summary>
        /// Clears the input text without triggering a suggestion lookup.
        /// </summary>
        private void ClearInputText()
        {
            this.SetInputText(string.Empty);
            this.RefreshSuggestions(false);
        }

        /// <summary>
        /// Moves the highlighted suggestion by the supplied offset, clamped to the ends of the list.
        /// </summary>
        /// <param name="offset">The number of items to move; negative values move up.</param>
        private void MoveSuggestionSelection(int offset)
        {
            if (_suggestionList == null || this.FilteredSuggestions.Count == 0)
            {
                return;
            }

            int nextIndex = _suggestionList.SelectedIndex < 0 ? 0 : _suggestionList.SelectedIndex + offset;
            nextIndex = Math.Clamp(nextIndex, 0, this.FilteredSuggestions.Count - 1);
            _suggestionList.SelectedIndex = nextIndex;
            _suggestionList.ScrollIntoView(_suggestionList.SelectedItem);
            _isSuggestionHighlighted = true;
        }

        /// <summary>
        /// Rebuilds <see cref="FilteredSuggestions"/> from <see cref="SuggestionsSource"/> for the current input text
        /// and opens or closes the drop-down accordingly.
        /// </summary>
        /// <param name="openDropDown">Whether the drop-down may be opened when matches are found.</param>
        private void RefreshSuggestions(bool openDropDown)
        {
            // Any immediate refresh supersedes a debounced one that has not fired yet.
            _suggestionTimer.Stop();

            // A fresh set of suggestions means nothing has been deliberately highlighted yet.
            _isSuggestionHighlighted = false;

            if (this.SuggestionsSource == null || !this.IsEnabled)
            {
                this.FilteredSuggestions.Clear();
                this.CloseSuggestionList();
                return;
            }

            string searchText = _textBox?.Text ?? string.Empty;

            if (searchText.Length < this.MinimumPrefixLength)
            {
                this.FilteredSuggestions.Clear();
                this.CloseSuggestionList();
                return;
            }

            this.FilteredSuggestions.Clear();

            foreach (var item in this.SuggestionsSource)
            {
                if (item == null)
                {
                    continue;
                }

                string text = item.ToString() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(text) || !this.MatchesFilter(item, text, searchText))
                {
                    continue;
                }

                if (this.ExcludeExistingTagsFromSuggestions && this.ContainsTag(text))
                {
                    continue;
                }

                this.FilteredSuggestions.Add(text);

                if (this.FilteredSuggestions.Count >= this.MaxSuggestionCount)
                {
                    break;
                }
            }

            if (_suggestionList != null)
            {
                _suggestionList.SelectedIndex = this.FilteredSuggestions.Count > 0 ? 0 : -1;
            }

            this.SetCurrentValue(IsSuggestionListOpenProperty, openDropDown && _popup != null && this.FilteredSuggestions.Count > 0);
        }

        /// <summary>
        /// Determines whether a suggestion matches the typed text according to <see cref="SuggestionFilterMode"/>.
        /// </summary>
        /// <param name="item">The original item from <see cref="SuggestionsSource"/>.</param>
        /// <param name="itemText">The item's display text.</param>
        /// <param name="searchText">The text currently typed into the input box.</param>
        private bool MatchesFilter(object item, string itemText, string searchText)
        {
            if (this.SuggestionFilterMode == AutoCompleteBoxFilterMode.Custom)
            {
                return this.SuggestionFilterPredicate?.Invoke(item, searchText) ?? true;
            }

            if (searchText.Length == 0)
            {
                return true;
            }

            return this.SuggestionFilterMode switch
            {
                AutoCompleteBoxFilterMode.StartsWith => itemText.StartsWith(searchText, true, CultureInfo.CurrentCulture),
                _ => CultureInfo.CurrentCulture.CompareInfo.IndexOf(itemText, searchText, CompareOptions.IgnoreCase) >= 0
            };
        }

        /// <summary>
        /// Rewires collection change notifications when the <see cref="SuggestionsSource"/> instance is swapped.
        /// </summary>
        private static void OnSuggestionsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TagBox)d;

            if (control._suggestionsCollectionChanged != null)
            {
                CollectionChangedEventManager.RemoveHandler(control._suggestionsCollectionChanged, control.OnSuggestionsCollectionChanged);
                control._suggestionsCollectionChanged = null;
            }

            if (e.NewValue is INotifyCollectionChanged collectionChanged)
            {
                control._suggestionsCollectionChanged = collectionChanged;
                CollectionChangedEventManager.AddHandler(collectionChanged, control.OnSuggestionsCollectionChanged);
            }

            control.RefreshSuggestions(control.IsSuggestionListOpen);
        }

        /// <summary>
        /// Refreshes the drop-down when the backing suggestion collection changes.
        /// </summary>
        private void OnSuggestionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            this.RefreshSuggestions(this.IsSuggestionListOpen);
        }

        /// <summary>
        /// Refreshes the drop-down whenever a property that affects filtering changes.
        /// </summary>
        private static void OnSuggestionBehaviorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((TagBox)d).RefreshSuggestions(((TagBox)d).IsSuggestionListOpen);
        }

        /// <summary>
        /// Notifies automation clients when the drop-down expands or collapses.
        /// </summary>
        private static void OnIsSuggestionListOpenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (UIElementAutomationPeer.FromElement((TagBox)d) is TagBoxAutomationPeer peer)
            {
                peer.RaiseExpandCollapseStateChanged((bool)e.OldValue, (bool)e.NewValue);
            }
        }

        /// <summary>
        /// Retargets the debounce timer when <see cref="SuggestionDelay"/> changes.
        /// </summary>
        private static void OnSuggestionDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (TagBox)d;
            control._suggestionTimer.Stop();
            control._suggestionTimer.Interval = (TimeSpan)e.NewValue;
        }

        private static object CoerceNonNegativeTimeSpan(DependencyObject d, object baseValue)
        {
            var value = (TimeSpan)baseValue;
            return value < TimeSpan.Zero ? TimeSpan.Zero : value;
        }

        private static object CoerceNonNegativeInt(DependencyObject d, object baseValue)
        {
            return Math.Max(0, (int)baseValue);
        }

        private static object CoercePositiveInt(DependencyObject d, object baseValue)
        {
            return Math.Max(1, (int)baseValue);
        }

        /// <summary>
        /// Determines whether the supplied focus target lives inside the given element, walking the visual tree and
        /// falling back to the logical tree so popup content counts as a descendant.
        /// </summary>
        private static bool IsDescendantFocus(DependencyObject root, object? focus)
        {
            if (focus is not DependencyObject dependencyObject)
            {
                return false;
            }

            DependencyObject? current = dependencyObject;

            while (current != null)
            {
                if (ReferenceEquals(current, root))
                {
                    return true;
                }

                current = current is Visual or System.Windows.Media.Media3D.Visual3D
                    ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return false;
        }

        /// <summary>
        /// Walks up the tree looking for the first ancestor of the requested type.
        /// </summary>
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
                    ? VisualTreeHelper.GetParent(current) ?? LogicalTreeHelper.GetParent(current)
                    : LogicalTreeHelper.GetParent(current);
            }

            return null;
        }
    }
}
