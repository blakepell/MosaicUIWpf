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

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;
using System.Windows.Automation.Peers;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Displays one day as a scrollable, continuously positioned timeline of calendar events.
    /// </summary>
    /// <remarks>
    /// Source objects are read through the configurable path properties. Dragging never writes to a source
    /// object directly; it raises <see cref="EventTimeChanged"/> and executes <see cref="EventTimeChangedCommand"/>
    /// with a <see cref="CalendarEventTimeChangedEventArgs"/> proposal.
    /// </remarks>
    [DefaultProperty(nameof(ItemsSource))]
    [DefaultEvent(nameof(EventTimeChanged))]
    [TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartTimelinePanel, Type = typeof(DayTimelinePanel))]
    public class DayCalendarView : Control
    {
        private const string PartScrollViewer = "PART_ScrollViewer";
        private const string PartTimelinePanel = "PART_TimelinePanel";
        private static readonly CultureInfo TimeCulture = CultureInfo.GetCultureInfo("en-US");
        private static readonly Dictionary<(Type Type, string Path), PropertyInfo[]?> AccessorCache = [];
        private static readonly object AccessorCacheLock = new();

        private readonly DispatcherTimer _currentTimeTimer;
        private readonly List<INotifyPropertyChanged> _observedItems = [];
        private ScrollViewer? _scrollViewer;
        private DayTimelinePanel? _timelinePanel;
        private INotifyCollectionChanged? _observableCollection;
        private CalendarEventPresenter? _draggedPresenter;
        private double _dragPointerOffsetMinutes;
        private bool _sourceSubscriptionsAttached;

        /// <summary>
        /// Identifies the <see cref="SelectedDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(
            nameof(SelectedDate), typeof(DateTime), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(DateTime.Today, OnDataPropertyChanged, CoerceSelectedDate));

        /// <summary>
        /// Identifies the <see cref="ItemsSource"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(DayCalendarView),
            new PropertyMetadata(null, OnItemsSourceChanged));

        /// <summary>
        /// Identifies the <see cref="SelectedItem"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem), typeof(object), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        /// <summary>
        /// Identifies the <see cref="EventTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventTemplateProperty = DependencyProperty.Register(
            nameof(EventTemplate), typeof(DataTemplate), typeof(DayCalendarView),
            new PropertyMetadata(null, OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventCommandProperty = DependencyProperty.Register(
            nameof(EventCommand), typeof(ICommand), typeof(DayCalendarView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="EventTimeChangedCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventTimeChangedCommandProperty = DependencyProperty.Register(
            nameof(EventTimeChangedCommand), typeof(ICommand), typeof(DayCalendarView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="EventDeletingCommand"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventDeletingCommandProperty = DependencyProperty.Register(
            nameof(EventDeletingCommand), typeof(ICommand), typeof(DayCalendarView), new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HourHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourHeightProperty = DependencyProperty.Register(
            nameof(HourHeight), typeof(double), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(80.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnTimelinePropertyChanged), IsPositiveFinite);

        /// <summary>
        /// Identifies the <see cref="TimeColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeColumnWidthProperty = DependencyProperty.Register(
            nameof(TimeColumnWidth), typeof(double), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(72.0, FrameworkPropertyMetadataOptions.AffectsMeasure, OnTimelinePropertyChanged), IsPositiveFinite);

        /// <summary>
        /// Identifies the <see cref="EventSpacing"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventSpacingProperty = DependencyProperty.Register(
            nameof(EventSpacing), typeof(double), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(3.0, FrameworkPropertyMetadataOptions.AffectsArrange, OnTimelinePropertyChanged), IsNonNegativeFinite);

        /// <summary>
        /// Identifies the <see cref="EventBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventBackgroundProperty = DependencyProperty.Register(
            nameof(EventBackground), typeof(Brush), typeof(DayCalendarView),
            new PropertyMetadata(Brushes.SteelBlue, OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventBorderBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventBorderBrushProperty = DependencyProperty.Register(
            nameof(EventBorderBrush), typeof(Brush), typeof(DayCalendarView),
            new PropertyMetadata(Brushes.DodgerBlue, OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventBorderThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventBorderThicknessProperty = DependencyProperty.Register(
            nameof(EventBorderThickness), typeof(Thickness), typeof(DayCalendarView),
            new PropertyMetadata(new Thickness(1), OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventCornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventCornerRadiusProperty = DependencyProperty.Register(
            nameof(EventCornerRadius), typeof(CornerRadius), typeof(DayCalendarView),
            new PropertyMetadata(new CornerRadius(0), OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventPadding"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EventPaddingProperty = DependencyProperty.Register(
            nameof(EventPadding), typeof(Thickness), typeof(DayCalendarView),
            new PropertyMetadata(new Thickness(8, 5, 8, 5), OnPresenterVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowQuarterHourLines"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowQuarterHourLinesProperty = DependencyProperty.Register(
            nameof(ShowQuarterHourLines), typeof(bool), typeof(DayCalendarView),
            new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsRender, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ShowCurrentTimeIndicator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowCurrentTimeIndicatorProperty = DependencyProperty.Register(
            nameof(ShowCurrentTimeIndicator), typeof(bool), typeof(DayCalendarView),
            new PropertyMetadata(true, OnShowCurrentTimeIndicatorChanged));

        /// <summary>
        /// Identifies the <see cref="DragSnapInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DragSnapIntervalProperty = DependencyProperty.Register(
            nameof(DragSnapInterval), typeof(TimeSpan), typeof(DayCalendarView),
            new PropertyMetadata(TimeSpan.FromMinutes(15)), IsValidSnapInterval);

        /// <summary>
        /// Identifies the <see cref="AllowEventDragging"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowEventDraggingProperty = DependencyProperty.Register(
            nameof(AllowEventDragging), typeof(bool), typeof(DayCalendarView), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="AllowCrossDayEvents"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowCrossDayEventsProperty = DependencyProperty.Register(
            nameof(AllowCrossDayEvents), typeof(bool), typeof(DayCalendarView), new PropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="InitialScrollTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InitialScrollTimeProperty = DependencyProperty.Register(
            nameof(InitialScrollTime), typeof(TimeSpan), typeof(DayCalendarView),
            new PropertyMetadata(TimeSpan.FromHours(8)), IsTimeWithinDay);

        /// <summary>
        /// Identifies the <see cref="ScrollToInitialTimeOnLoad"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ScrollToInitialTimeOnLoadProperty = DependencyProperty.Register(
            nameof(ScrollToInitialTimeOnLoad), typeof(bool), typeof(DayCalendarView), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="StartDatePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty StartDatePathProperty = DependencyProperty.Register(
            nameof(StartDatePath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("StartDate", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EndDatePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EndDatePathProperty = DependencyProperty.Register(
            nameof(EndDatePath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("EndDate", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TitlePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitlePathProperty = DependencyProperty.Register(
            nameof(TitlePath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("Title", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DescriptionPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DescriptionPathProperty = DependencyProperty.Register(
            nameof(DescriptionPath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("Description", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="BackgroundPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty BackgroundPathProperty = DependencyProperty.Register(
            nameof(BackgroundPath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("Background", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="IsReadOnlyPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyPathProperty = DependencyProperty.Register(
            nameof(IsReadOnlyPath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("IsReadOnly", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="CanDeletePath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanDeletePathProperty = DependencyProperty.Register(
            nameof(CanDeletePath), typeof(string), typeof(DayCalendarView), new PropertyMetadata("CanDelete", OnDataPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="HourLineBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourLineBrushProperty = DependencyProperty.Register(
            nameof(HourLineBrush), typeof(Brush), typeof(DayCalendarView), new PropertyMetadata(Brushes.Gray, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="QuarterHourLineBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty QuarterHourLineBrushProperty = DependencyProperty.Register(
            nameof(QuarterHourLineBrush), typeof(Brush), typeof(DayCalendarView), new PropertyMetadata(Brushes.LightGray, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="CurrentTimeBrush"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CurrentTimeBrushProperty = DependencyProperty.Register(
            nameof(CurrentTimeBrush), typeof(Brush), typeof(DayCalendarView), new PropertyMetadata(Brushes.Red, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TimeForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeForegroundProperty = DependencyProperty.Register(
            nameof(TimeForeground), typeof(Brush), typeof(DayCalendarView), new PropertyMetadata(Brushes.Gray, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="TimeColumnBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TimeColumnBackgroundProperty = DependencyProperty.Register(
            nameof(TimeColumnBackground), typeof(Brush), typeof(DayCalendarView),
            new PropertyMetadata(Brushes.Transparent, OnTimelinePropertyChanged));

        /// <summary>
        /// Identifies the <see cref="EventTimeChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent EventTimeChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(EventTimeChanged), RoutingStrategy.Bubble, typeof(EventHandler<CalendarEventTimeChangedEventArgs>), typeof(DayCalendarView));

        /// <summary>
        /// Identifies the <see cref="EventDeleting"/> routed event.
        /// </summary>
        public static readonly RoutedEvent EventDeletingEvent = EventManager.RegisterRoutedEvent(
            nameof(EventDeleting), RoutingStrategy.Bubble, typeof(EventHandler<CalendarEventDeletingEventArgs>), typeof(DayCalendarView));

        /// <summary>
        /// Gets or sets the date represented by the timeline.
        /// </summary>
        [Category("Common")]
        public DateTime SelectedDate
        {
            get => (DateTime)GetValue(SelectedDateProperty);
            set => SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Gets or sets the event objects displayed by the calendar.
        /// </summary>
        [Category("Common")]
        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the selected source event object.
        /// </summary>
        [Category("Common")]
        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// Gets or sets the optional template whose data context is the original source event object.
        /// </summary>
        [Category("Appearance")]
        public DataTemplate? EventTemplate
        {
            get => (DataTemplate?)GetValue(EventTemplateProperty);
            set => SetValue(EventTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets the command executed with the original source event when an event is activated.
        /// </summary>
        [Category("Action")]
        public ICommand? EventCommand
        {
            get => (ICommand?)GetValue(EventCommandProperty);
            set => SetValue(EventCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command executed with a <see cref="CalendarEventTimeChangedEventArgs"/> drag proposal.
        /// </summary>
        [Category("Action")]
        public ICommand? EventTimeChangedCommand
        {
            get => (ICommand?)GetValue(EventTimeChangedCommandProperty);
            set => SetValue(EventTimeChangedCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the command executed with a <see cref="CalendarEventDeletingEventArgs"/> delete proposal.
        /// </summary>
        /// <remarks>
        /// The command runs after <see cref="EventDeleting"/> and may set
        /// <see cref="CalendarEventDeletingEventArgs.Cancel"/> to <see langword="true"/> to abandon the deletion.
        /// </remarks>
        [Category("Action")]
        public ICommand? EventDeletingCommand
        {
            get => (ICommand?)GetValue(EventDeletingCommandProperty);
            set => SetValue(EventDeletingCommandProperty, value);
        }

        /// <summary>
        /// Gets or sets the number of device-independent pixels used for one hour.
        /// </summary>
        [Category("Layout")]
        public double HourHeight
        {
            get => (double)GetValue(HourHeightProperty);
            set => SetValue(HourHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the width of the time-label column.
        /// </summary>
        [Category("Layout")]
        public double TimeColumnWidth
        {
            get => (double)GetValue(TimeColumnWidthProperty);
            set => SetValue(TimeColumnWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the gap between adjacent event columns.
        /// </summary>
        [Category("Layout")]
        public double EventSpacing
        {
            get => (double)GetValue(EventSpacingProperty);
            set => SetValue(EventSpacingProperty, value);
        }

        /// <summary>
        /// Gets or sets the default event-card background.
        /// </summary>
        [Category("Brushes")]
        public Brush EventBackground
        {
            get => (Brush)GetValue(EventBackgroundProperty);
            set => SetValue(EventBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the default event-card border brush.
        /// </summary>
        [Category("Brushes")]
        public Brush EventBorderBrush
        {
            get => (Brush)GetValue(EventBorderBrushProperty);
            set => SetValue(EventBorderBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the default event-card border thickness.
        /// </summary>
        [Category("Appearance")]
        public Thickness EventBorderThickness
        {
            get => (Thickness)GetValue(EventBorderThicknessProperty);
            set => SetValue(EventBorderThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the default event-card corner radius.
        /// </summary>
        [Category("Appearance")]
        public CornerRadius EventCornerRadius
        {
            get => (CornerRadius)GetValue(EventCornerRadiusProperty);
            set => SetValue(EventCornerRadiusProperty, value);
        }

        /// <summary>
        /// Gets or sets the default event-card content padding.
        /// </summary>
        [Category("Appearance")]
        public Thickness EventPadding
        {
            get => (Thickness)GetValue(EventPaddingProperty);
            set => SetValue(EventPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether 15-minute grid lines are shown.
        /// </summary>
        [Category("Appearance")]
        public bool ShowQuarterHourLines
        {
            get => (bool)GetValue(ShowQuarterHourLinesProperty);
            set => SetValue(ShowQuarterHourLinesProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether today's current-time line is shown.
        /// </summary>
        [Category("Appearance")]
        public bool ShowCurrentTimeIndicator
        {
            get => (bool)GetValue(ShowCurrentTimeIndicatorProperty);
            set => SetValue(ShowCurrentTimeIndicatorProperty, value);
        }

        /// <summary>
        /// Gets or sets the interval used to snap drag proposals.
        /// </summary>
        [Category("Behavior")]
        public TimeSpan DragSnapInterval
        {
            get => (TimeSpan)GetValue(DragSnapIntervalProperty);
            set => SetValue(DragSnapIntervalProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether source events can be dragged.
        /// </summary>
        [Category("Behavior")]
        public bool AllowEventDragging
        {
            get => (bool)GetValue(AllowEventDraggingProperty);
            set => SetValue(AllowEventDraggingProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether a dragged event may end after the displayed day.
        /// </summary>
        [Category("Behavior")]
        public bool AllowCrossDayEvents
        {
            get => (bool)GetValue(AllowCrossDayEventsProperty);
            set => SetValue(AllowCrossDayEventsProperty, value);
        }

        /// <summary>
        /// Gets or sets the time shown near the top when the calendar initially loads.
        /// </summary>
        [Category("Behavior")]
        public TimeSpan InitialScrollTime
        {
            get => (TimeSpan)GetValue(InitialScrollTimeProperty);
            set => SetValue(InitialScrollTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets a value that indicates whether the initial time is scrolled into view on load.
        /// </summary>
        [Category("Behavior")]
        public bool ScrollToInitialTimeOnLoad
        {
            get => (bool)GetValue(ScrollToInitialTimeOnLoadProperty);
            set => SetValue(ScrollToInitialTimeOnLoadProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves each event's start.
        /// </summary>
        [Category("Data")]
        public string StartDatePath
        {
            get => (string)GetValue(StartDatePathProperty);
            set => SetValue(StartDatePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves each event's end.
        /// </summary>
        [Category("Data")]
        public string EndDatePath
        {
            get => (string)GetValue(EndDatePathProperty);
            set => SetValue(EndDatePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves each event's title.
        /// </summary>
        [Category("Data")]
        public string TitlePath
        {
            get => (string)GetValue(TitlePathProperty);
            set => SetValue(TitlePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves each event's optional description.
        /// </summary>
        [Category("Data")]
        public string DescriptionPath
        {
            get => (string)GetValue(DescriptionPathProperty);
            set => SetValue(DescriptionPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves each event's optional background brush.
        /// </summary>
        [Category("Data")]
        public string BackgroundPath
        {
            get => (string)GetValue(BackgroundPathProperty);
            set => SetValue(BackgroundPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves whether each event cannot be moved by dragging.
        /// </summary>
        /// <value>
        /// A property path whose resolved Boolean value disables dragging when <see langword="true"/>.
        /// Missing properties and non-Boolean values are treated as <see langword="false"/>. The default is
        /// <c>IsReadOnly</c>.
        /// </value>
        [Category("Data")]
        public string IsReadOnlyPath
        {
            get => (string)GetValue(IsReadOnlyPathProperty);
            set => SetValue(IsReadOnlyPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the property path that resolves whether each event can be deleted.
        /// </summary>
        /// <value>
        /// A property path whose resolved Boolean value disables deletion when <see langword="false"/>.
        /// Missing properties and non-Boolean values are treated as <see langword="true"/>. The default is
        /// <c>CanDelete</c>.
        /// </value>
        [Category("Data")]
        public string CanDeletePath
        {
            get => (string)GetValue(CanDeletePathProperty);
            set => SetValue(CanDeletePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for hour separators.
        /// </summary>
        [Category("Brushes")]
        public Brush HourLineBrush
        {
            get => (Brush)GetValue(HourLineBrushProperty);
            set => SetValue(HourLineBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for quarter-hour separators.
        /// </summary>
        [Category("Brushes")]
        public Brush QuarterHourLineBrush
        {
            get => (Brush)GetValue(QuarterHourLineBrushProperty);
            set => SetValue(QuarterHourLineBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for the current-time and drag-time guides.
        /// </summary>
        [Category("Brushes")]
        public Brush CurrentTimeBrush
        {
            get => (Brush)GetValue(CurrentTimeBrushProperty);
            set => SetValue(CurrentTimeBrushProperty, value);
        }

        /// <summary>
        /// Gets or sets the brush used for time labels.
        /// </summary>
        [Category("Brushes")]
        public Brush TimeForeground
        {
            get => (Brush)GetValue(TimeForegroundProperty);
            set => SetValue(TimeForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background brush used by the protected time-label column.
        /// </summary>
        [Category("Brushes")]
        public Brush TimeColumnBackground
        {
            get => (Brush)GetValue(TimeColumnBackgroundProperty);
            set => SetValue(TimeColumnBackgroundProperty, value);
        }

        /// <summary>
        /// Occurs when dragging proposes a new time range for an event.
        /// </summary>
        [Category("Behavior")]
        public event EventHandler<CalendarEventTimeChangedEventArgs> EventTimeChanged
        {
            add => AddHandler(EventTimeChangedEvent, value);
            remove => RemoveHandler(EventTimeChangedEvent, value);
        }

        /// <summary>
        /// Occurs before a focused event is deleted, allowing a handler to cancel the removal.
        /// </summary>
        [Category("Behavior")]
        public event EventHandler<CalendarEventDeletingEventArgs> EventDeleting
        {
            add => AddHandler(EventDeletingEvent, value);
            remove => RemoveHandler(EventDeletingEvent, value);
        }

        internal DayTimelinePanel? TimelinePanel => _timelinePanel;

        internal bool IsDragActive => _draggedPresenter != null;

        internal DateTime DragPreviewStart { get; private set; }

        internal DateTime DragPreviewEnd { get; private set; }

        static DayCalendarView()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(DayCalendarView),
                new FrameworkPropertyMetadata(typeof(DayCalendarView)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DayCalendarView"/> class.
        /// </summary>
        public DayCalendarView()
        {
            _currentTimeTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMinutes(1)
            };
            _currentTimeTimer.Tick += OnCurrentTimeTimerTick;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            if (_timelinePanel != null)
            {
                _timelinePanel.Owner = null;
                _timelinePanel.Children.Clear();
            }

            base.OnApplyTemplate();
            _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
            _timelinePanel = GetTemplateChild(PartTimelinePanel) as DayTimelinePanel;

            if (_timelinePanel != null)
            {
                _timelinePanel.Owner = this;
            }

            RebuildPresenters();
        }

        /// <summary>
        /// Scrolls the timeline so the specified time is near its top edge.
        /// </summary>
        /// <param name="time">A time between midnight and the end of the day.</param>
        public void ScrollToTime(TimeSpan time)
        {
            var minutes = Math.Clamp(time.TotalMinutes, 0.0, (24.0 * 60.0) - 1.0);
            _scrollViewer?.ScrollToVerticalOffset(minutes * HourHeight / 60.0);
        }

        /// <summary>
        /// Scrolls the timeline to the current local time.
        /// </summary>
        public void ScrollToCurrentTime()
        {
            ScrollToTime(DateTime.Now.TimeOfDay);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new DayCalendarViewAutomationPeer(this);
        }

        internal void ActivateEvent(CalendarEventPresenter presenter)
        {
            SetCurrentValue(SelectedItemProperty, presenter.EventItem);
            var command = EventCommand;
            if (command?.CanExecute(presenter.EventItem) == true)
            {
                command.Execute(presenter.EventItem);
            }
        }

        /// <summary>
        /// Raises <see cref="EventDeleting"/> and executes <see cref="EventDeletingCommand"/> for the specified
        /// source event, removing it from <see cref="ItemsSource"/> when the proposal is not cancelled.
        /// </summary>
        /// <param name="calendarEvent">The source object supplied by the calendar's items source.</param>
        /// <returns>
        /// <see langword="true"/> when the deletion was accepted; <see langword="false"/> when a handler or the
        /// command cancelled it.
        /// </returns>
        /// <remarks>
        /// The event is removed only when <see cref="ItemsSource"/> is a mutable, non-fixed-size <see cref="IList"/>.
        /// Other sources are left untouched so the handler can perform the removal itself.
        /// </remarks>
        public bool DeleteEvent(object calendarEvent)
        {
            ArgumentNullException.ThrowIfNull(calendarEvent);

            var args = new CalendarEventDeletingEventArgs(calendarEvent)
            {
                RoutedEvent = EventDeletingEvent,
                Source = this
            };

            RaiseEvent(args);

            var command = EventDeletingCommand;
            if (command?.CanExecute(args) == true)
            {
                command.Execute(args);
            }

            if (args.Cancel)
            {
                return false;
            }

            RemoveFromSource(calendarEvent);
            return true;
        }

        internal void RequestDeleteEvent(CalendarEventPresenter presenter)
        {
            if (!presenter.CanDelete)
            {
                return;
            }

            DeleteEvent(presenter.EventItem);
        }

        internal bool BeginEventDrag(CalendarEventPresenter presenter, double pointerDownY)
        {
            if (!AllowEventDragging || presenter.IsReadOnly || _timelinePanel == null || presenter.ActualEnd <= presenter.ActualStart)
            {
                return false;
            }

            var duration = presenter.ActualEnd - presenter.ActualStart;
            if (!AllowCrossDayEvents && duration > TimeSpan.FromDays(1))
            {
                return false;
            }

            var pixelsPerMinute = HourHeight / 60.0;
            var visibleTop = (presenter.ClippedStart - SelectedDate.Date).TotalMinutes * pixelsPerMinute;
            _dragPointerOffsetMinutes = (pointerDownY - visibleTop) / pixelsPerMinute;
            _draggedPresenter = presenter;
            DragPreviewStart = presenter.ActualStart;
            DragPreviewEnd = presenter.ActualEnd;
            presenter.SetDragging(true);
            SetCurrentValue(SelectedItemProperty, presenter.EventItem);
            _timelinePanel.RefreshTimeline();
            return true;
        }

        internal void UpdateEventDrag(double pointerY)
        {
            if (_draggedPresenter == null || _timelinePanel == null)
            {
                return;
            }

            var duration = _draggedPresenter.ActualEnd - _draggedPresenter.ActualStart;
            var proposedMinutes = (pointerY * 60.0 / HourHeight) - _dragPointerOffsetMinutes;
            var snapMinutes = DragSnapInterval.TotalMinutes;
            var snappedMinutes = Math.Round(proposedMinutes / snapMinutes, MidpointRounding.AwayFromZero) * snapMinutes;
            var maximumStart = AllowCrossDayEvents
                ? (24.0 * 60.0) - snapMinutes
                : Math.Max(0.0, (24.0 * 60.0) - duration.TotalMinutes);
            maximumStart = Math.Floor(maximumStart / snapMinutes) * snapMinutes;
            snappedMinutes = Math.Clamp(snappedMinutes, 0.0, maximumStart);

            DragPreviewStart = SelectedDate.Date.AddMinutes(snappedMinutes);
            DragPreviewEnd = DragPreviewStart + duration;
            _timelinePanel.RefreshTimeline();
        }

        internal void CompleteEventDrag()
        {
            var presenter = _draggedPresenter;
            if (presenter == null)
            {
                return;
            }

            var args = new CalendarEventTimeChangedEventArgs(
                presenter.EventItem,
                presenter.ActualStart,
                presenter.ActualEnd,
                DragPreviewStart,
                DragPreviewEnd)
            {
                RoutedEvent = EventTimeChangedEvent,
                Source = this
            };

            EndDragVisuals(presenter);
            RaiseEvent(args);

            var command = EventTimeChangedCommand;
            if (command?.CanExecute(args) == true)
            {
                command.Execute(args);
            }
        }

        internal void CancelEventDrag()
        {
            if (_draggedPresenter != null)
            {
                EndDragVisuals(_draggedPresenter);
            }
        }

        private static bool IsPositiveFinite(object value) => value is double number && double.IsFinite(number) && number > 0.0;

        private static bool IsNonNegativeFinite(object value) => value is double number && double.IsFinite(number) && number >= 0.0;

        private static bool IsValidSnapInterval(object value) => value is TimeSpan interval && interval > TimeSpan.Zero && interval <= TimeSpan.FromDays(1);

        private static bool IsTimeWithinDay(object value) => value is TimeSpan time && time >= TimeSpan.Zero && time < TimeSpan.FromDays(1);

        private static object CoerceSelectedDate(DependencyObject dependencyObject, object baseValue) => ((DateTime)baseValue).Date;

        private static void OnItemsSourceChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var calendar = (DayCalendarView)dependencyObject;
            calendar.DetachSourceSubscriptions();
            calendar.AttachSourceSubscriptions();
            calendar.RebuildPresenters();
        }

        private static void OnDataPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DayCalendarView)dependencyObject).RebuildPresenters();
        }

        private static void OnSelectedItemChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DayCalendarView)dependencyObject).UpdateSelection();
        }

        private static void OnPresenterVisualPropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DayCalendarView)dependencyObject).UpdatePresenterVisuals();
        }

        private static void OnTimelinePropertyChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            ((DayCalendarView)dependencyObject)._timelinePanel?.RefreshTimeline();
        }

        private static void OnShowCurrentTimeIndicatorChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e)
        {
            var calendar = (DayCalendarView)dependencyObject;
            calendar.UpdateTimerState();
            calendar._timelinePanel?.InvalidateVisual();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AttachSourceSubscriptions();
            UpdateTimerState();
            RebuildPresenters();

            if (ScrollToInitialTimeOnLoad)
            {
                Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() => ScrollToTime(InitialScrollTime)));
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CancelEventDrag();
            _currentTimeTimer.Stop();
            DetachSourceSubscriptions();
        }

        private void OnCurrentTimeTimerTick(object? sender, EventArgs e)
        {
            _timelinePanel?.InvalidateVisual();
        }

        private void UpdateTimerState()
        {
            if (IsLoaded && ShowCurrentTimeIndicator)
            {
                _currentTimeTimer.Start();
            }
            else
            {
                _currentTimeTimer.Stop();
            }
        }

        private void AttachSourceSubscriptions()
        {
            if (_sourceSubscriptionsAttached || !IsLoaded)
            {
                return;
            }

            _sourceSubscriptionsAttached = true;
            if (ItemsSource is INotifyCollectionChanged observableCollection)
            {
                _observableCollection = observableCollection;
                _observableCollection.CollectionChanged += OnSourceCollectionChanged;
            }

            SubscribeToItemChanges();
        }

        private void DetachSourceSubscriptions()
        {
            if (_observableCollection != null)
            {
                _observableCollection.CollectionChanged -= OnSourceCollectionChanged;
                _observableCollection = null;
            }

            foreach (var item in _observedItems)
            {
                item.PropertyChanged -= OnSourceItemPropertyChanged;
            }

            _observedItems.Clear();
            _sourceSubscriptionsAttached = false;
        }

        private void SubscribeToItemChanges()
        {
            foreach (var item in _observedItems)
            {
                item.PropertyChanged -= OnSourceItemPropertyChanged;
            }

            _observedItems.Clear();
            if (ItemsSource == null)
            {
                return;
            }

            foreach (var item in ItemsSource)
            {
                if (item is INotifyPropertyChanged observableItem && !_observedItems.Contains(observableItem))
                {
                    observableItem.PropertyChanged += OnSourceItemPropertyChanged;
                    _observedItems.Add(observableItem);
                }
            }
        }

        private void OnSourceCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            SubscribeToItemChanges();
            RebuildPresenters();
        }

        private void OnSourceItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            RebuildPresenters();
        }

        private void RebuildPresenters()
        {
            if (_timelinePanel == null)
            {
                return;
            }

            CancelEventDrag();
            _timelinePanel.Children.Clear();
            var source = ItemsSource;
            if (source == null)
            {
                _timelinePanel.RefreshTimeline();
                return;
            }

            var dayStart = SelectedDate.Date;
            var dayEnd = dayStart.AddDays(1);
            foreach (var item in source)
            {
                if (item == null || !TryGetDateTime(item, StartDatePath, out var start) ||
                    !TryGetDateTime(item, EndDatePath, out var end) || end <= start ||
                    end <= dayStart || start >= dayEnd)
                {
                    continue;
                }

                var presenter = new CalendarEventPresenter
                {
                    Owner = this,
                    Content = item,
                    ActualStart = start,
                    ActualEnd = end,
                    ClippedStart = start < dayStart ? dayStart : start,
                    ClippedEnd = end > dayEnd ? dayEnd : end,
                    DisplayTitle = GetPathValue(item, TitlePath)?.ToString() ?? string.Empty,
                    DisplayDescription = GetPathValue(item, DescriptionPath)?.ToString() ?? string.Empty,
                    DisplayTimeText = $"{start.ToString("h:mm tt", TimeCulture)} – {end.ToString("h:mm tt", TimeCulture)}"
                };

                ApplyPresenterVisuals(presenter);
                presenter.SetReadOnly(GetPathValue(item, IsReadOnlyPath) is true);
                presenter.SetCanDelete(GetPathValue(item, CanDeletePath) is not false);
                _timelinePanel.Children.Add(presenter);
            }

            UpdateSelection();
            _timelinePanel.RefreshTimeline();
        }

        private void UpdatePresenterVisuals()
        {
            if (_timelinePanel == null)
            {
                return;
            }

            foreach (UIElement child in _timelinePanel.Children)
            {
                if (child is CalendarEventPresenter presenter)
                {
                    ApplyPresenterVisuals(presenter);
                }
            }
        }

        private void ApplyPresenterVisuals(CalendarEventPresenter presenter)
        {
            presenter.ContentTemplate = EventTemplate;
            presenter.Background = GetPathValue(presenter.EventItem, BackgroundPath) as Brush ?? EventBackground;
            presenter.BorderBrush = EventBorderBrush;
            presenter.BorderThickness = EventBorderThickness;
            presenter.Padding = EventPadding;
            presenter.CornerRadius = EventCornerRadius;
            System.Windows.Automation.AutomationProperties.SetName(presenter, presenter.DisplayTitle);
        }

        private void UpdateSelection()
        {
            if (_timelinePanel == null)
            {
                return;
            }

            foreach (UIElement child in _timelinePanel.Children)
            {
                if (child is CalendarEventPresenter presenter)
                {
                    presenter.SetSelected(ReferenceEquals(presenter.EventItem, SelectedItem));
                }
            }
        }

        private void RemoveFromSource(object calendarEvent)
        {
            if (ReferenceEquals(SelectedItem, calendarEvent))
            {
                SetCurrentValue(SelectedItemProperty, null);
            }

            if (ItemsSource is not IList list || list.IsReadOnly || list.IsFixedSize || !list.Contains(calendarEvent))
            {
                return;
            }

            list.Remove(calendarEvent);

            // Observable sources rebuild through the collection-changed handler; plain lists do not notify.
            if (ItemsSource is not INotifyCollectionChanged)
            {
                SubscribeToItemChanges();
                RebuildPresenters();
            }
        }

        private void EndDragVisuals(CalendarEventPresenter presenter)
        {
            presenter.SetDragging(false);
            _draggedPresenter = null;
            _timelinePanel?.RefreshTimeline();
        }

        private static bool TryGetDateTime(object source, string path, out DateTime value)
        {
            var resolved = GetPathValue(source, path);
            if (resolved is DateTime dateTime)
            {
                value = dateTime;
                return true;
            }

            if (resolved is DateTimeOffset dateTimeOffset)
            {
                value = dateTimeOffset.LocalDateTime;
                return true;
            }

            value = default;
            return false;
        }

        private static object? GetPathValue(object source, string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return source;
            }

            var key = (source.GetType(), path);
            PropertyInfo[]? properties;
            lock (AccessorCacheLock)
            {
                if (!AccessorCache.TryGetValue(key, out properties))
                {
                    properties = BuildPropertyPath(key.Item1, path);
                    AccessorCache[key] = properties;
                }
            }

            if (properties == null)
            {
                return null;
            }

            object? current = source;
            foreach (var property in properties)
            {
                if (current == null)
                {
                    return null;
                }

                current = property.GetValue(current);
            }

            return current;
        }

        private static PropertyInfo[]? BuildPropertyPath(Type sourceType, string path)
        {
            var segments = path.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0)
            {
                return [];
            }

            var properties = new PropertyInfo[segments.Length];
            var currentType = sourceType;
            for (var index = 0; index < segments.Length; index++)
            {
                var property = currentType.GetProperty(
                    segments[index],
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
                if (property == null || property.GetIndexParameters().Length != 0)
                {
                    return null;
                }

                properties[index] = property;
                currentType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            }

            return properties;
        }
    }

    /// <summary>
    /// Exposes the day calendar as a list to UI Automation clients.
    /// </summary>
    internal sealed class DayCalendarViewAutomationPeer : FrameworkElementAutomationPeer
    {
        internal DayCalendarViewAutomationPeer(DayCalendarView owner)
            : base(owner)
        {
        }

        protected override string GetClassNameCore() => nameof(DayCalendarView);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.List;
    }
}
