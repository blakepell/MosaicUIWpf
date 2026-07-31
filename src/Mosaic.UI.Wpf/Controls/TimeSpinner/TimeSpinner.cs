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

using System.Globalization;
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A time entry control that chooses a time of day with three coordinated scrolling wheels for hour, minute,
    /// and AM or PM rather than with a clock face or a text mask. The closed control shows a compact entry surface
    /// with one field per component; clicking it opens a drop down containing the wheels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the twelve hour sibling of <see cref="DateSpinner"/> and shares its interaction model exactly: the
    /// same mouse, touch, and keyboard handling, the same wheel physics, and by default the same explicit Apply and
    /// Cancel commit. It also reuses <see cref="DateSpinnerSelector"/> as its wheel, which is why the wheel entries
    /// are <see cref="DateSpinnerItem"/> instances; the type is named for where it first appeared rather than for
    /// anything date specific in its behavior.
    /// </para>
    /// <para>
    /// <see cref="SelectedTime"/> is a nullable <see cref="TimeOnly"/>, always normalized to whole minutes. Models
    /// that store a time of day as a <see cref="TimeSpan"/>, which is what EF Core historically maps a SQL
    /// <c>time</c> column to, can bind <see cref="SelectedTimeSpan"/> instead. The two are mirrors of each other, so
    /// only one of them should be bound on any given instance.
    /// </para>
    /// <para>
    /// While the drop down is open the control tracks a separate temporary time, so opening and closing without
    /// touching a wheel never assigns a value. See <see cref="CommitMode"/> for how the temporary time reaches
    /// <see cref="SelectedTime"/>.
    /// </para>
    /// <para>
    /// Values outside <see cref="MinimumTime"/> and <see cref="MaximumTime"/>, and minutes that fall off
    /// <see cref="MinuteInterval"/>, are still rendered on the wheels but are not selectable, which keeps the
    /// wheels' geometry stable while the user moves across a boundary. An externally assigned
    /// <see cref="SelectedTime"/> is snapped onto the nearest offered value rather than rejected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:TimeSpinner Header="Appointment time"
    ///                     SelectedTime="{Binding AppointmentTime, Mode=TwoWay}"
    ///                     MinimumTime="8:00 AM"
    ///                     MaximumTime="5:00 PM"
    ///                     MinuteInterval="15"
    ///                     IsClearButtonVisible="True" /&gt;
    /// </code>
    /// </example>
    [TemplatePart(Name = PartRoot, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartHourSelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartMinuteSelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartMeridiemSelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartApplyButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartCancelButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartClearButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(SelectedTimeChanged))]
    [DefaultProperty(nameof(SelectedTime))]
    public class TimeSpinner : Control
    {
        private const string PartRoot = "PART_Root";
        private const string PartPopup = "PART_Popup";
        private const string PartHourSelector = "PART_HourSelector";
        private const string PartMinuteSelector = "PART_MinuteSelector";
        private const string PartMeridiemSelector = "PART_MeridiemSelector";
        private const string PartApplyButton = "PART_ApplyButton";
        private const string PartCancelButton = "PART_CancelButton";
        private const string PartClearButton = "PART_ClearButton";

        private FrameworkElement? _root;
        private Popup? _popup;
        private DateSpinnerSelector? _hourSelector;
        private DateSpinnerSelector? _minuteSelector;
        private DateSpinnerSelector? _meridiemSelector;
        private ButtonBase? _applyButton;
        private ButtonBase? _cancelButton;
        private ButtonBase? _clearButton;
        private Window? _hostWindow;

        /// <summary>
        /// The temporary time the wheels are editing while the drop down is open. Never written to
        /// <see cref="SelectedTime"/> unless the commit mode or the user asks for it.
        /// </summary>
        private TimeOnly _pendingTime = new(0, 0);

        /// <summary>
        /// The value of <see cref="SelectedTime"/> at the moment the drop down opened, used by <see cref="Cancel"/>.
        /// </summary>
        private TimeOnly? _timeBeforeOpen;

        /// <summary>
        /// Set while the control is pushing values between its own state and the wheels, so that the resulting
        /// change notifications do not loop back around.
        /// </summary>
        private bool _isSynchronizing;

        /// <summary>
        /// Set while the control is itself closing the popup, so that <see cref="Popup.Closed"/> can tell a
        /// deliberate close from a light dismiss.
        /// </summary>
        private bool _isClosingDeliberately;

        #region Resource Keys

        /// <summary>
        /// The resource key for the default hour placeholder text. Override this key in a merged resource
        /// dictionary to localize the placeholder without setting <see cref="HourPlaceholderText"/> on every
        /// instance.
        /// </summary>
        public static ComponentResourceKey HourPlaceholderTextKey { get; } = new(typeof(TimeSpinner), "HourPlaceholderText");

        /// <summary>
        /// The resource key for the default minute placeholder text.
        /// </summary>
        public static ComponentResourceKey MinutePlaceholderTextKey { get; } = new(typeof(TimeSpinner), "MinutePlaceholderText");

        /// <summary>
        /// The resource key for the default AM or PM placeholder text.
        /// </summary>
        public static ComponentResourceKey MeridiemPlaceholderTextKey { get; } = new(typeof(TimeSpinner), "MeridiemPlaceholderText");

        /// <summary>
        /// The resource key for the Apply button's caption.
        /// </summary>
        public static ComponentResourceKey ApplyButtonTextKey { get; } = new(typeof(TimeSpinner), "ApplyButtonText");

        /// <summary>
        /// The resource key for the Cancel button's caption.
        /// </summary>
        public static ComponentResourceKey CancelButtonTextKey { get; } = new(typeof(TimeSpinner), "CancelButtonText");

        /// <summary>
        /// The resource key for the Clear button's accessible name and tooltip.
        /// </summary>
        public static ComponentResourceKey ClearButtonTextKey { get; } = new(typeof(TimeSpinner), "ClearButtonText");

        #endregion

        #region Routed Commands

        /// <summary>
        /// Opens the drop down. Bound to the entry surface by the default template.
        /// </summary>
        public static RoutedUICommand OpenCommand { get; } = new("Open", nameof(OpenCommand), typeof(TimeSpinner));

        /// <summary>
        /// Closes the drop down without committing the temporary time.
        /// </summary>
        public static RoutedUICommand CloseCommand { get; } = new("Close", nameof(CloseCommand), typeof(TimeSpinner));

        /// <summary>
        /// Clears <see cref="SelectedTime"/>. Bound to the clear button by the default template.
        /// </summary>
        public static RoutedUICommand ClearCommand { get; } = new("Clear", nameof(ClearCommand), typeof(TimeSpinner));

        /// <summary>
        /// Commits the temporary time and closes the drop down. Bound to the Apply button by the default template.
        /// </summary>
        public static RoutedUICommand ApplyCommand { get; } = new("Apply", nameof(ApplyCommand), typeof(TimeSpinner));

        /// <summary>
        /// Restores the time that was selected when the drop down opened and closes it. Bound to the Cancel button
        /// by the default template.
        /// </summary>
        public static RoutedUICommand CancelCommand { get; } = new("Cancel", nameof(CancelCommand), typeof(TimeSpinner));

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SelectedTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
            nameof(SelectedTime), typeof(TimeOnly?), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeChanged, CoerceSelectedTime));

        /// <summary>
        /// The selected time of day, or <see langword="null"/> when the control is unset. Always normalized to whole
        /// minutes and always snapped onto the nearest value the wheels offer, which means inside
        /// <see cref="MinimumTime"/> to <see cref="MaximumTime"/> and on <see cref="MinuteInterval"/>.
        /// </summary>
        /// <remarks>
        /// The property carries a <see cref="TimeOnlyConverter"/> so a literal such as <c>8:30 AM</c> can be written
        /// directly in XAML. <see cref="TimeOnly"/> has no converter registered on the type itself, so without this
        /// the parser would have no way to read the literal.
        /// </remarks>
        [Category("Mosaic")]
        [Description("The selected time of day, or null when nothing is selected.")]
        [TypeConverter(typeof(TimeOnlyConverter))]
        public TimeOnly? SelectedTime
        {
            get => (TimeOnly?)this.GetValue(SelectedTimeProperty);
            set => this.SetValue(SelectedTimeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="SelectedTimeSpan"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedTimeSpanProperty = DependencyProperty.Register(
            nameof(SelectedTimeSpan), typeof(TimeSpan?), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedTimeSpanChanged));

        /// <summary>
        /// <see cref="SelectedTime"/> expressed as a <see cref="TimeSpan"/> measured from midnight, for models that
        /// store a time of day that way. Writing either property updates the other, so bind one or the other rather
        /// than both.
        /// </summary>
        /// <remarks>
        /// The value written here is the value <see cref="SelectedTime"/> ends up with, so a time that has to be
        /// snapped onto the minute step is reflected back through this property too. A <see cref="TimeSpan"/> that
        /// falls outside a single day is folded into one before it is applied.
        /// </remarks>
        [Category("Mosaic")]
        [Description("The selected time expressed as a TimeSpan from midnight. Mirrors SelectedTime.")]
        public TimeSpan? SelectedTimeSpan
        {
            get => (TimeSpan?)this.GetValue(SelectedTimeSpanProperty);
            set => this.SetValue(SelectedTimeSpanProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinimumTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumTimeProperty = DependencyProperty.Register(
            nameof(MinimumTime), typeof(TimeOnly), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(new TimeOnly(0, 0), OnRangeChanged, CoerceTimeOnly));

        /// <summary>
        /// The earliest selectable time, inclusive. Defaults to midnight.
        /// </summary>
        [Category("Mosaic")]
        [Description("The earliest selectable time, inclusive.")]
        [TypeConverter(typeof(TimeOnlyConverter))]
        public TimeOnly MinimumTime
        {
            get => (TimeOnly)this.GetValue(MinimumTimeProperty);
            set => this.SetValue(MinimumTimeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaximumTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumTimeProperty = DependencyProperty.Register(
            nameof(MaximumTime), typeof(TimeOnly), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(new TimeOnly(23, 59), OnRangeChanged, CoerceMaximumTime));

        /// <summary>
        /// The latest selectable time, inclusive. Defaults to 11:59 PM. An assignment earlier than
        /// <see cref="MinimumTime"/> is coerced up to it rather than throwing, so the invariant
        /// <c>MinimumTime &lt;= MaximumTime</c> always holds.
        /// </summary>
        [Category("Mosaic")]
        [Description("The latest selectable time, inclusive. Coerced up to MinimumTime when set below it.")]
        [TypeConverter(typeof(TimeOnlyConverter))]
        public TimeOnly MaximumTime
        {
            get => (TimeOnly)this.GetValue(MaximumTimeProperty);
            set => this.SetValue(MaximumTimeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinuteInterval"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteIntervalProperty = DependencyProperty.Register(
            nameof(MinuteInterval), typeof(int), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(1, OnMinuteIntervalChanged, CoerceMinuteInterval));

        /// <summary>
        /// The step between the minutes the wheel offers. One, the default, offers every minute; fifteen offers
        /// 00, 15, 30, and 45. Coerced into 1 through 60.
        /// </summary>
        /// <remarks>
        /// The step restarts every hour rather than running from midnight, so a step that does not divide sixty
        /// evenly simply produces a short last gap in each hour. An existing <see cref="SelectedTime"/> that no
        /// longer falls on the step is snapped onto the nearest value that does.
        /// </remarks>
        [Category("Mosaic")]
        [Description("The step between the minutes the wheel offers. Coerced into 1 through 60.")]
        public int MinuteInterval
        {
            get => (int)this.GetValue(MinuteIntervalProperty);
            set => this.SetValue(MinuteIntervalProperty, value);
        }

        private static readonly DependencyPropertyKey IsDropDownOpenPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsDropDownOpen), typeof(bool), typeof(TimeSpinner), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="IsDropDownOpen"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDropDownOpenProperty = IsDropDownOpenPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether the wheel drop down is open. Read only, use <see cref="Open"/> and <see cref="Close"/> or the
        /// matching routed commands.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the wheel drop down is open.")]
        public bool IsDropDownOpen
        {
            get => (bool)this.GetValue(IsDropDownOpenProperty);
            private set => this.SetValue(IsDropDownOpenPropertyKey, value);
        }

        /// <summary>
        /// Identifies the <see cref="CommitMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CommitModeProperty = DependencyProperty.Register(
            nameof(CommitMode), typeof(TimeSpinnerCommitMode), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(TimeSpinnerCommitMode.Explicit));

        /// <summary>
        /// When edits made on the wheels are written back to <see cref="SelectedTime"/>. Defaults to
        /// <see cref="TimeSpinnerCommitMode.Explicit"/>, matching the accept/dismiss model of the WinUI 3 picker.
        /// </summary>
        [Category("Mosaic")]
        [Description("When wheel edits are written back to SelectedTime.")]
        public TimeSpinnerCommitMode CommitMode
        {
            get => (TimeSpinnerCommitMode)this.GetValue(CommitModeProperty);
            set => this.SetValue(CommitModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LightDismissBehavior"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LightDismissBehaviorProperty = DependencyProperty.Register(
            nameof(LightDismissBehavior), typeof(TimeSpinnerLightDismissBehavior), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(TimeSpinnerLightDismissBehavior.Cancel));

        /// <summary>
        /// What clicking outside an open drop down does with the temporary time. Defaults to
        /// <see cref="TimeSpinnerLightDismissBehavior.Cancel"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("What clicking outside an open drop down does with the temporary time.")]
        public TimeSpinnerLightDismissBehavior LightDismissBehavior
        {
            get => (TimeSpinnerLightDismissBehavior)this.GetValue(LightDismissBehaviorProperty);
            set => this.SetValue(LightDismissBehaviorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsMinuteVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinuteVisibleProperty = DependencyProperty.Register(
            nameof(IsMinuteVisible), typeof(bool), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(true, OnFieldVisibilityChanged));

        /// <summary>
        /// Whether the minute field is shown. Hiding it turns the control into an hour picker and preserves the
        /// minute component of the selected or temporary time. The hour and AM/PM fields are always shown, because
        /// a time missing either of them cannot be read.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the minute field is shown.")]
        public bool IsMinuteVisible
        {
            get => (bool)this.GetValue(IsMinuteVisibleProperty);
            set => this.SetValue(IsMinuteVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MeridiemDisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MeridiemDisplayModeProperty = DependencyProperty.Register(
            nameof(MeridiemDisplayMode), typeof(TimeSpinnerMeridiemDisplayMode), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(TimeSpinnerMeridiemDisplayMode.Culture, OnFormatChanged));

        /// <summary>
        /// How the AM and PM designators are cased. Defaults to whatever the culture publishes.
        /// </summary>
        [Category("Mosaic")]
        [Description("How the AM and PM designators are cased.")]
        public TimeSpinnerMeridiemDisplayMode MeridiemDisplayMode
        {
            get => (TimeSpinnerMeridiemDisplayMode)this.GetValue(MeridiemDisplayModeProperty);
            set => this.SetValue(MeridiemDisplayModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HourFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourFormatProperty = DependencyProperty.Register(
            nameof(HourFormat), typeof(string), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// An optional .NET time format string applied to hour values, for example <c>hh</c> for a padded hour.
        /// Defaults to the unpadded number. A format string the runtime cannot honor falls back to that default
        /// rather than throwing.
        /// </summary>
        [Category("Mosaic")]
        [Description("An optional .NET time format string applied to hour values.")]
        public string? HourFormat
        {
            get => (string?)this.GetValue(HourFormatProperty);
            set => this.SetValue(HourFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinuteFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteFormatProperty = DependencyProperty.Register(
            nameof(MinuteFormat), typeof(string), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// An optional .NET time format string applied to minute values. Defaults to two digits.
        /// </summary>
        [Category("Mosaic")]
        [Description("An optional .NET time format string applied to minute values.")]
        public string? MinuteFormat
        {
            get => (string?)this.GetValue(MinuteFormatProperty);
            set => this.SetValue(MinuteFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Culture"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
            nameof(Culture), typeof(CultureInfo), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The culture supplying the AM and PM designators and number formatting. Defaults to
        /// <see cref="CultureInfo.CurrentUICulture"/> when <see langword="null"/>.
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="DateSpinner"/>, culture does not reorder the fields. Hour, minute, AM/PM is what a
        /// twelve hour clock means, and cultures that would order it differently do not write twelve hour times at
        /// all.
        /// </remarks>
        [Category("Mosaic")]
        [Description("The culture supplying the AM and PM designators and number formatting.")]
        public CultureInfo? Culture
        {
            get => (CultureInfo?)this.GetValue(CultureProperty);
            set => this.SetValue(CultureProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(TimeSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Optional content rendered above the entry surface. When it is a string it also becomes the control's
        /// accessible name.
        /// </summary>
        [Category("Mosaic")]
        [Description("Optional content rendered above the entry surface.")]
        public object? Header
        {
            get => this.GetValue(HeaderProperty);
            set => this.SetValue(HeaderProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HeaderTemplate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderTemplateProperty = DependencyProperty.Register(
            nameof(HeaderTemplate), typeof(DataTemplate), typeof(TimeSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// The template used to render <see cref="Header"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("The template used to render the header.")]
        public DataTemplate? HeaderTemplate
        {
            get => (DataTemplate?)this.GetValue(HeaderTemplateProperty);
            set => this.SetValue(HeaderTemplateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="HourPlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourPlaceholderTextProperty = DependencyProperty.Register(
            nameof(HourPlaceholderText), typeof(string), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the hour field when nothing is selected. Falls back to the
        /// <see cref="HourPlaceholderTextKey"/> resource so it can be localized centrally.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the hour field when nothing is selected.")]
        public string? HourPlaceholderText
        {
            get => (string?)this.GetValue(HourPlaceholderTextProperty);
            set => this.SetValue(HourPlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinutePlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinutePlaceholderTextProperty = DependencyProperty.Register(
            nameof(MinutePlaceholderText), typeof(string), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the minute field when nothing is selected. Falls back to the
        /// <see cref="MinutePlaceholderTextKey"/> resource.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the minute field when nothing is selected.")]
        public string? MinutePlaceholderText
        {
            get => (string?)this.GetValue(MinutePlaceholderTextProperty);
            set => this.SetValue(MinutePlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MeridiemPlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MeridiemPlaceholderTextProperty = DependencyProperty.Register(
            nameof(MeridiemPlaceholderText), typeof(string), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the AM/PM field when nothing is selected. Falls back to the
        /// <see cref="MeridiemPlaceholderTextKey"/> resource.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the AM/PM field when nothing is selected.")]
        public string? MeridiemPlaceholderText
        {
            get => (string?)this.GetValue(MeridiemPlaceholderTextProperty);
            set => this.SetValue(MeridiemPlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsClearButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClearButtonVisibleProperty = DependencyProperty.Register(
            nameof(IsClearButtonVisible), typeof(bool), typeof(TimeSpinner), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Whether the entry surface shows a clear button. The button is only enabled while a time is selected and
        /// the control is editable.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the entry surface shows a clear button.")]
        public bool IsClearButtonVisible
        {
            get => (bool)this.GetValue(IsClearButtonVisibleProperty);
            set => this.SetValue(IsClearButtonVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = DependencyProperty.Register(
            nameof(IsReadOnly), typeof(bool), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));

        /// <summary>
        /// Whether the control is read only. The selected time stays visible and the control stays focusable, but
        /// the drop down cannot be opened and the value cannot be changed or cleared. Setting this while the drop
        /// down is open closes it.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the value can be changed. A read only control still shows its time and takes focus.")]
        public bool IsReadOnly
        {
            get => (bool)this.GetValue(IsReadOnlyProperty);
            set => this.SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(TimeSpinner), new FrameworkPropertyMetadata(30.0));

        /// <summary>
        /// The height of a single entry on the wheels.
        /// </summary>
        [Category("Mosaic")]
        [Description("The height of a single entry on the wheels.")]
        public double ItemHeight
        {
            get => (double)this.GetValue(ItemHeightProperty);
            set => this.SetValue(ItemHeightProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="VisibleItemCount"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty VisibleItemCountProperty = DependencyProperty.Register(
            nameof(VisibleItemCount), typeof(int), typeof(TimeSpinner), new FrameworkPropertyMetadata(5));

        /// <summary>
        /// How many entries each wheel shows at once. Coerced by the wheels to an odd number of at least three.
        /// </summary>
        [Category("Mosaic")]
        [Description("How many entries each wheel shows at once.")]
        public int VisibleItemCount
        {
            get => (int)this.GetValue(VisibleItemCountProperty);
            set => this.SetValue(VisibleItemCountProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsScrollAnimationEnabled"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsScrollAnimationEnabledProperty = DependencyProperty.Register(
            nameof(IsScrollAnimationEnabled), typeof(bool), typeof(TimeSpinner), new FrameworkPropertyMetadata(true));

        /// <summary>
        /// Whether the wheels ease into position using the <see cref="InertiaScrollViewer"/> in their template.
        /// Set to <see langword="false"/> for instant movement when motion should be reduced.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the wheels ease into position rather than jumping.")]
        public bool IsScrollAnimationEnabled
        {
            get => (bool)this.GetValue(IsScrollAnimationEnabledProperty);
            set => this.SetValue(IsScrollAnimationEnabledProperty, value);
        }

        #endregion

        #region Read Only Presentation Properties

        private static readonly DependencyPropertyKey HourItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HourItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(TimeSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="HourItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourItemsProperty = HourItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The entries offered by the hour wheel, ordered 12 then 1 through 11 so that scrolling down moves forward
        /// in time. All twelve are always present; a change that only affects which of them are selectable mutates
        /// the existing entries in place.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? HourItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(HourItemsProperty);
            private set => this.SetValue(HourItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MinuteItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MinuteItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(TimeSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="MinuteItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteItemsProperty = MinuteItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The entries offered by the minute wheel, sized by <see cref="MinuteInterval"/>.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? MinuteItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(MinuteItemsProperty);
            private set => this.SetValue(MinuteItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MeridiemItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MeridiemItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(TimeSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="MeridiemItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MeridiemItemsProperty = MeridiemItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The two entries offered by the AM/PM wheel, carrying the numeric values of
        /// <see cref="TimeSpinnerMeridiem"/>.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? MeridiemItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(MeridiemItemsProperty);
            private set => this.SetValue(MeridiemItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HourTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HourText), typeof(string), typeof(TimeSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="HourText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HourTextProperty = HourTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The hour field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string HourText
        {
            get => (string)this.GetValue(HourTextProperty);
            private set => this.SetValue(HourTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MinuteTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MinuteText), typeof(string), typeof(TimeSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="MinuteText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteTextProperty = MinuteTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The minute field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string MinuteText
        {
            get => (string)this.GetValue(MinuteTextProperty);
            private set => this.SetValue(MinuteTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MeridiemTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MeridiemText), typeof(string), typeof(TimeSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="MeridiemText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MeridiemTextProperty = MeridiemTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The AM/PM field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string MeridiemText
        {
            get => (string)this.GetValue(MeridiemTextProperty);
            private set => this.SetValue(MeridiemTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HasSelectedTimePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasSelectedTime), typeof(bool), typeof(TimeSpinner), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="HasSelectedTime"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasSelectedTimeProperty = HasSelectedTimePropertyKey.DependencyProperty;

        /// <summary>
        /// Whether a time is currently selected. The template uses this to render placeholders differently from
        /// real values, so the unset state does not rely on color alone.
        /// </summary>
        [Browsable(false)]
        public bool HasSelectedTime
        {
            get => (bool)this.GetValue(HasSelectedTimeProperty);
            private set => this.SetValue(HasSelectedTimePropertyKey, value);
        }

        private static readonly DependencyPropertyKey MinuteColumnWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MinuteColumnWidth), typeof(GridLength), typeof(TimeSpinner),
            new FrameworkPropertyMetadata(new GridLength(1, GridUnitType.Star)));

        /// <summary>
        /// Identifies the <see cref="MinuteColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinuteColumnWidthProperty = MinuteColumnWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// The width of the minute column. Collapses to zero when <see cref="IsMinuteVisible"/> is
        /// <see langword="false"/> so the remaining fields close the gap instead of leaving a hole.
        /// </summary>
        [Browsable(false)]
        public GridLength MinuteColumnWidth
        {
            get => (GridLength)this.GetValue(MinuteColumnWidthProperty);
            private set => this.SetValue(MinuteColumnWidthPropertyKey, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="SelectedTimeChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SelectedTimeChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(SelectedTimeChanged), RoutingStrategy.Bubble, typeof(EventHandler<TimeSpinnerTimeChangedEventArgs>), typeof(TimeSpinner));

        /// <summary>
        /// Occurs when <see cref="SelectedTime"/> changes to a different minute. Assigning a value that normalizes
        /// or snaps to the minute already selected raises nothing.
        /// </summary>
        public event EventHandler<TimeSpinnerTimeChangedEventArgs> SelectedTimeChanged
        {
            add => this.AddHandler(SelectedTimeChangedEvent, value);
            remove => this.RemoveHandler(SelectedTimeChangedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimeSpinner));

        /// <summary>
        /// Occurs after the wheel drop down opens.
        /// </summary>
        public event RoutedEventHandler DropDownOpened
        {
            add => this.AddHandler(DropDownOpenedEvent, value);
            remove => this.RemoveHandler(DropDownOpenedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownClosedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TimeSpinner));

        /// <summary>
        /// Occurs after the wheel drop down closes, however it was closed.
        /// </summary>
        public event RoutedEventHandler DropDownClosed
        {
            add => this.AddHandler(DropDownClosedEvent, value);
            remove => this.RemoveHandler(DropDownClosedEvent, value);
        }

        #endregion

        /// <summary>
        /// Initializes static metadata for the <see cref="TimeSpinner"/> class.
        /// </summary>
        static TimeSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(TimeSpinner), new FrameworkPropertyMetadata(typeof(TimeSpinner)));
            FocusableProperty.OverrideMetadata(typeof(TimeSpinner), new FrameworkPropertyMetadata(true));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(TimeSpinner), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeSpinner"/> class.
        /// </summary>
        public TimeSpinner()
        {
            this.CommandBindings.Add(new CommandBinding(OpenCommand, (_, e) => { this.Open(); e.Handled = true; }, (_, e) => e.CanExecute = this.CanInteract));
            this.CommandBindings.Add(new CommandBinding(CloseCommand, (_, e) => { this.Close(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));
            this.CommandBindings.Add(new CommandBinding(ClearCommand, (_, e) => { this.Clear(); e.Handled = true; }, (_, e) => e.CanExecute = this.CanInteract && this.SelectedTime.HasValue));
            this.CommandBindings.Add(new CommandBinding(ApplyCommand, (_, e) => { this.Apply(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));
            this.CommandBindings.Add(new CommandBinding(CancelCommand, (_, e) => { this.Cancel(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));

            this.Loaded += this.OnSpinnerLoaded;
            this.Unloaded += this.OnSpinnerUnloaded;

            this.RefreshColumnWidths();
            this.RebuildAllItems();
            this.RefreshDisplayText();
        }

        /// <summary>
        /// The effective culture, which is <see cref="Culture"/> when set and
        /// <see cref="CultureInfo.CurrentUICulture"/> otherwise.
        /// </summary>
        public CultureInfo EffectiveCulture => this.Culture ?? CultureInfo.CurrentUICulture;

        /// <summary>
        /// Whether the control currently accepts changes from the user.
        /// </summary>
        private bool CanInteract => this.IsEnabled && !this.IsReadOnly;

        #region Template

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            this.DetachTemplateParts();

            base.OnApplyTemplate();

            _root = this.GetTemplateChild(PartRoot) as FrameworkElement;
            _popup = this.GetTemplateChild(PartPopup) as Popup;
            _hourSelector = this.GetTemplateChild(PartHourSelector) as DateSpinnerSelector;
            _minuteSelector = this.GetTemplateChild(PartMinuteSelector) as DateSpinnerSelector;
            _meridiemSelector = this.GetTemplateChild(PartMeridiemSelector) as DateSpinnerSelector;
            _applyButton = this.GetTemplateChild(PartApplyButton) as ButtonBase;
            _cancelButton = this.GetTemplateChild(PartCancelButton) as ButtonBase;
            _clearButton = this.GetTemplateChild(PartClearButton) as ButtonBase;

            if (_root != null)
            {
                _root.MouseLeftButtonUp += this.OnRootMouseLeftButtonUp;
                _root.TouchUp += this.OnRootTouchUp;
            }

            if (_popup != null)
            {
                _popup.Opened += this.OnPopupOpened;
                _popup.Closed += this.OnPopupClosed;
            }

            foreach (var selector in this.Selectors())
            {
                selector.ValueChanged += this.OnSelectorValueChanged;
                selector.PreviewKeyDown += this.OnSelectorPreviewKeyDown;
            }

            this.SyncSelectorsToPending();
        }

        /// <summary>
        /// Removes every handler attached to the previous template's parts. Called before a new template is
        /// applied so that a retemplated control does not keep the old visuals alive.
        /// </summary>
        private void DetachTemplateParts()
        {
            if (_root != null)
            {
                _root.MouseLeftButtonUp -= this.OnRootMouseLeftButtonUp;
                _root.TouchUp -= this.OnRootTouchUp;
                _root = null;
            }

            if (_popup != null)
            {
                _popup.Opened -= this.OnPopupOpened;
                _popup.Closed -= this.OnPopupClosed;
                _popup = null;
            }

            foreach (var selector in this.Selectors())
            {
                selector.ValueChanged -= this.OnSelectorValueChanged;
                selector.PreviewKeyDown -= this.OnSelectorPreviewKeyDown;
            }

            _hourSelector = null;
            _minuteSelector = null;
            _meridiemSelector = null;
            _applyButton = null;
            _cancelButton = null;
            _clearButton = null;
        }

        /// <summary>
        /// The wheels that exist in the current template, in display order.
        /// </summary>
        private IEnumerable<DateSpinnerSelector> Selectors()
        {
            if (_hourSelector != null)
            {
                yield return _hourSelector;
            }

            if (_minuteSelector != null)
            {
                yield return _minuteSelector;
            }

            if (_meridiemSelector != null)
            {
                yield return _meridiemSelector;
            }
        }

        /// <summary>
        /// The wheels that are currently visible, in display order. Used for Left and Right navigation.
        /// </summary>
        private List<DateSpinnerSelector> VisibleSelectors()
        {
            var result = new List<DateSpinnerSelector>(3);

            if (_hourSelector != null)
            {
                result.Add(_hourSelector);
            }

            if (this.IsMinuteVisible && _minuteSelector != null)
            {
                result.Add(_minuteSelector);
            }

            if (_meridiemSelector != null)
            {
                result.Add(_meridiemSelector);
            }

            return result;
        }

        private void OnSpinnerLoaded(object sender, RoutedEventArgs e)
        {
            // Keep the drop down glued to the control when the window moves or resizes. A WPF popup does not
            // follow its placement target on its own.
            _hostWindow = Window.GetWindow(this);

            if (_hostWindow != null)
            {
                _hostWindow.LocationChanged += this.OnHostWindowChanged;
                _hostWindow.SizeChanged += this.OnHostWindowChanged;
            }
        }

        private void OnSpinnerUnloaded(object sender, RoutedEventArgs e)
        {
            if (_hostWindow != null)
            {
                _hostWindow.LocationChanged -= this.OnHostWindowChanged;
                _hostWindow.SizeChanged -= this.OnHostWindowChanged;
                _hostWindow = null;
            }

            // A popup lives in its own window, so leaving it open while the owner is torn down would strand it.
            if (this.IsDropDownOpen)
            {
                this.Close();
            }
        }

        private void OnHostWindowChanged(object? sender, EventArgs e)
        {
            if (_popup is not { IsOpen: true })
            {
                return;
            }

            // Nudging an offset is the supported way to make a popup recompute its placement.
            double offset = _popup.HorizontalOffset;
            _popup.HorizontalOffset = offset + 1;
            _popup.HorizontalOffset = offset;
        }

        #endregion

        #region Public API

        /// <summary>
        /// Opens the wheel drop down. Does nothing when the control is disabled, read only, or already open.
        /// </summary>
        /// <remarks>
        /// The temporary time starts at <see cref="SelectedTime"/>, or at the current time of day snapped into the
        /// permitted range when nothing is selected. Opening never assigns <see cref="SelectedTime"/>.
        /// </remarks>
        public void Open()
        {
            if (this.IsDropDownOpen || !this.CanInteract)
            {
                return;
            }

            _timeBeforeOpen = this.SelectedTime;
            _pendingTime = this.SelectedTime ?? this.SnapToOffered(TimeOnly.FromDateTime(DateTime.Now));

            this.RebuildAllItems();
            this.SyncSelectorsToPending();

            this.IsDropDownOpen = true;

            if (_popup != null)
            {
                _popup.IsOpen = true;
            }

            this.RaiseEvent(new RoutedEventArgs(DropDownOpenedEvent, this));
        }

        /// <summary>
        /// Closes the wheel drop down without committing the temporary time. In
        /// <see cref="TimeSpinnerCommitMode.Explicit"/> mode the temporary time is discarded; in
        /// <see cref="TimeSpinnerCommitMode.Immediate"/> mode the value has already been written through and is kept.
        /// </summary>
        public void Close()
        {
            this.CloseDropDown();
        }

        /// <summary>
        /// Commits the temporary time to <see cref="SelectedTime"/> and closes the drop down. Does nothing when
        /// the drop down is not open.
        /// </summary>
        public void Apply()
        {
            if (!this.IsDropDownOpen)
            {
                return;
            }

            this.SetSelectedTime(_pendingTime);
            this.CloseDropDown();
        }

        /// <summary>
        /// Restores the time that was selected when the drop down opened and closes it. Does nothing when the drop
        /// down is not open.
        /// </summary>
        /// <remarks>
        /// This also undoes changes already written through in <see cref="TimeSpinnerCommitMode.Immediate"/> mode,
        /// which is why the mode's own light dismiss path calls <see cref="Close"/> rather than this.
        /// </remarks>
        public void Cancel()
        {
            if (!this.IsDropDownOpen)
            {
                return;
            }

            this.SetSelectedTime(_timeBeforeOpen);
            this.CloseDropDown();
        }

        /// <summary>
        /// Clears the selection by setting <see cref="SelectedTime"/> to <see langword="null"/>. Does nothing when
        /// the control is read only or disabled.
        /// </summary>
        public void Clear()
        {
            if (!this.CanInteract)
            {
                return;
            }

            this.SetSelectedTime(null);
        }

        #endregion

        #region Drop Down Lifecycle

        /// <summary>
        /// Closes the popup and returns focus to the entry surface.
        /// </summary>
        private void CloseDropDown()
        {
            if (!this.IsDropDownOpen)
            {
                return;
            }

            _isClosingDeliberately = true;

            try
            {
                if (_popup != null)
                {
                    _popup.IsOpen = false;
                }
            }
            finally
            {
                _isClosingDeliberately = false;
            }

            this.IsDropDownOpen = false;

            // Focus must come back to something the user can see, otherwise it is stranded on a popup that no
            // longer exists.
            if (this.Focusable)
            {
                this.Focus();
            }

            this.RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
        }

        private void OnPopupOpened(object? sender, EventArgs e)
        {
            // Land the keyboard on the first visible wheel so arrow keys work without a click first.
            var selectors = this.VisibleSelectors();

            if (selectors.Count > 0)
            {
                var first = selectors[0];
                this.Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => first.Focus()));
            }
        }

        private void OnPopupClosed(object? sender, EventArgs e)
        {
            if (_isClosingDeliberately || !this.IsDropDownOpen)
            {
                return;
            }

            // The popup dismissed itself, which means the user clicked away from it.
            if (this.CommitMode == TimeSpinnerCommitMode.Explicit && this.LightDismissBehavior == TimeSpinnerLightDismissBehavior.Apply)
            {
                this.SetSelectedTime(_pendingTime);
            }

            this.IsDropDownOpen = false;

            if (this.Focusable)
            {
                this.Focus();
            }

            this.RaiseEvent(new RoutedEventArgs(DropDownClosedEvent, this));
        }

        #endregion

        #region Property Change Handling

        private static object CoerceTimeOnly(DependencyObject d, object baseValue)
        {
            return TimeSpinnerClock.Normalize((TimeOnly)baseValue);
        }

        private static object CoerceMaximumTime(DependencyObject d, object baseValue)
        {
            var spinner = (TimeSpinner)d;
            var value = TimeSpinnerClock.Normalize((TimeOnly)baseValue);
            var minimum = spinner.MinimumTime;

            // The range invariant is maintained by lifting the maximum rather than by throwing, so a view model
            // that publishes both bounds can update them in either order.
            return value < minimum ? minimum : value;
        }

        private static object CoerceMinuteInterval(DependencyObject d, object baseValue)
        {
            return TimeSpinnerClock.CoerceInterval((int)baseValue);
        }

        private static object CoerceSelectedTime(DependencyObject d, object? baseValue)
        {
            if (baseValue is not TimeOnly value)
            {
                return null!;
            }

            return ((TimeSpinner)d).SnapToOffered(value);
        }

        private static void OnSelectedTimeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;
            var oldTime = (TimeOnly?)e.OldValue;
            var newTime = (TimeOnly?)e.NewValue;

            spinner.HasSelectedTime = newTime.HasValue;
            spinner.RefreshDisplayText();

            // The two value properties mirror each other. Writing a value equal to the one already there is a no-op
            // in WPF, so the pair converges after at most one correction rather than looping.
            spinner.SelectedTimeSpan = newTime?.ToTimeSpan();

            // While the drop down is open the wheels are the source of truth, so an external assignment only
            // reshapes them when it did not originate there.
            if (!spinner._isSynchronizing && newTime.HasValue)
            {
                spinner._pendingTime = newTime.Value;
                spinner.RebuildAllItems();
                spinner.SyncSelectorsToPending();
            }

            spinner.RaiseEvent(new TimeSpinnerTimeChangedEventArgs(SelectedTimeChangedEvent, spinner, oldTime, newTime));

            if (UIElementAutomationPeer.FromElement(spinner) is TimeSpinnerAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldTime, newTime);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnSelectedTimeSpanChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            if (e.NewValue is not TimeSpan span)
            {
                spinner.SelectedTime = null;
                return;
            }

            // A duration is not a time of day, so anything outside a single day is folded back into one rather than
            // rejected. Negative durations wrap backwards from midnight, which is what a user of a TimeSpan valued
            // model would expect from, for example, minus one hour.
            long ticks = span.Ticks % TimeSpan.TicksPerDay;

            if (ticks < 0)
            {
                ticks += TimeSpan.TicksPerDay;
            }

            spinner.SelectedTime = TimeOnly.FromTimeSpan(TimeSpan.FromTicks(ticks));
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            // A new minimum can invalidate the maximum, and both can invalidate the selection.
            spinner.CoerceValue(MaximumTimeProperty);
            spinner.CoerceValue(SelectedTimeProperty);

            spinner._pendingTime = spinner.SnapToOffered(spinner._pendingTime);
            spinner.RebuildAllItems();
            spinner.SyncSelectorsToPending();
            spinner.RefreshDisplayText();
        }

        private static void OnMinuteIntervalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            // A coarser step can leave the current selection between two offered minutes.
            spinner.CoerceValue(SelectedTimeProperty);

            spinner._pendingTime = spinner.SnapToOffered(spinner._pendingTime);
            spinner.RebuildAllItems();
            spinner.SyncSelectorsToPending();
            spinner.RefreshDisplayText();
        }

        private static void OnFieldVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            spinner.RefreshColumnWidths();
            spinner.RefreshDisplayText();
        }

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            spinner.RefreshItemText();
            spinner.RefreshDisplayText();
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (TimeSpinner)d;

            if (spinner.IsReadOnly && spinner.IsDropDownOpen)
            {
                spinner.Close();
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Selection

        /// <summary>
        /// Moves a time onto the nearest value this spinner's wheels actually offer.
        /// </summary>
        private TimeOnly SnapToOffered(TimeOnly value)
        {
            return TimeSpinnerClock.Snap(value, this.MinimumTime, this.MaximumTime, this.MinuteInterval);
        }

        /// <summary>
        /// Writes a time to <see cref="SelectedTime"/>, snapping it first and suppressing the write entirely when
        /// the effective minute has not changed. This is what keeps duplicate <see cref="SelectedTimeChanged"/>
        /// notifications from reaching consumers.
        /// </summary>
        private void SetSelectedTime(TimeOnly? value)
        {
            var snapped = value.HasValue ? this.SnapToOffered(value.Value) : (TimeOnly?)null;

            if (Nullable.Equals(this.SelectedTime, snapped))
            {
                return;
            }

            _isSynchronizing = true;

            try
            {
                this.SelectedTime = snapped;
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        private void OnSelectorValueChanged(object? sender, EventArgs e)
        {
            if (_isSynchronizing || sender is not DateSpinnerSelector selector)
            {
                return;
            }

            int hour = TimeSpinnerClock.ToHour12(_pendingTime);
            int minute = _pendingTime.Minute;
            var meridiem = TimeSpinnerClock.ToMeridiem(_pendingTime);

            if (ReferenceEquals(selector, _hourSelector) && selector.SelectedNumber.HasValue)
            {
                hour = selector.SelectedNumber.Value;
            }
            else if (ReferenceEquals(selector, _minuteSelector) && selector.SelectedNumber.HasValue)
            {
                minute = selector.SelectedNumber.Value;
            }
            else if (ReferenceEquals(selector, _meridiemSelector) && selector.SelectedNumber.HasValue)
            {
                meridiem = selector.SelectedNumber.Value == (int)TimeSpinnerMeridiem.Pm
                    ? TimeSpinnerMeridiem.Pm
                    : TimeSpinnerMeridiem.Am;
            }
            else
            {
                return;
            }

            _pendingTime = this.SnapToOffered(TimeSpinnerClock.Compose(hour, minute, meridiem));

            this.RebuildAllItems();
            this.SyncSelectorsToPending();

            if (this.CommitMode == TimeSpinnerCommitMode.Immediate)
            {
                this.SetSelectedTime(_pendingTime);
            }
        }

        /// <summary>
        /// Pushes the temporary time onto the wheels without letting the resulting change notifications feed back
        /// into <see cref="OnSelectorValueChanged"/>.
        /// </summary>
        private void SyncSelectorsToPending()
        {
            _isSynchronizing = true;

            try
            {
                if (_hourSelector != null)
                {
                    _hourSelector.SelectedNumber = TimeSpinnerClock.ToHour12(_pendingTime);
                }

                if (_minuteSelector != null)
                {
                    _minuteSelector.SelectedNumber = _pendingTime.Minute;
                }

                if (_meridiemSelector != null)
                {
                    _meridiemSelector.SelectedNumber = (int)TimeSpinnerClock.ToMeridiem(_pendingTime);
                }
            }
            finally
            {
                _isSynchronizing = false;
            }
        }

        #endregion

        #region Item Generation

        /// <summary>
        /// Rebuilds the three wheels for the current temporary time and range. The collections are only re-created
        /// when their shape changes, otherwise the existing entries are updated in place so the wheels do not lose
        /// their realized containers on every keystroke.
        /// </summary>
        private void RebuildAllItems()
        {
            this.RebuildHourItems();
            this.RebuildMinuteItems();
            this.RebuildMeridiemItems();
        }

        private void RebuildHourItems()
        {
            var culture = this.EffectiveCulture;
            var meridiem = TimeSpinnerClock.ToMeridiem(_pendingTime);
            var hours = TimeSpinnerClock.GetHourValues();
            var existing = this.HourItems;

            // All twelve hours are always present; only which of them can be chosen changes with the range and the
            // currently selected half of the day.
            if (existing == null || existing.Count != hours.Count)
            {
                var items = new List<DateSpinnerItem>(hours.Count);

                foreach (int hour in hours)
                {
                    items.Add(new DateSpinnerItem(
                        hour,
                        TimeSpinnerClock.FormatHour(hour, meridiem, culture, this.HourFormat),
                        TimeSpinnerClock.IsHourSelectable(hour, meridiem, this.MinimumTime, this.MaximumTime, this.MinuteInterval)));
                }

                this.HourItems = items;
                return;
            }

            foreach (var item in existing)
            {
                item.IsSelectable = TimeSpinnerClock.IsHourSelectable(item.Value, meridiem, this.MinimumTime, this.MaximumTime, this.MinuteInterval);

                // A custom HourFormat can include the designator, so the text follows the half of the day.
                item.DisplayText = TimeSpinnerClock.FormatHour(item.Value, meridiem, culture, this.HourFormat);
            }
        }

        private void RebuildMinuteItems()
        {
            var culture = this.EffectiveCulture;
            var meridiem = TimeSpinnerClock.ToMeridiem(_pendingTime);
            int hour = TimeSpinnerClock.ToHour12(_pendingTime);
            var minutes = TimeSpinnerClock.GetMinuteValues(this.MinuteInterval);
            var existing = this.MinuteItems;

            // The minute collection is only re-created when the step changes, which is what makes moving between
            // two hours free.
            if (existing == null || existing.Count != minutes.Count)
            {
                var items = new List<DateSpinnerItem>(minutes.Count);

                foreach (int minute in minutes)
                {
                    items.Add(new DateSpinnerItem(
                        minute,
                        TimeSpinnerClock.FormatMinute(minute, culture, this.MinuteFormat),
                        TimeSpinnerClock.IsMinuteSelectable(hour, minute, meridiem, this.MinimumTime, this.MaximumTime)));
                }

                this.MinuteItems = items;
                return;
            }

            foreach (var item in existing)
            {
                item.IsSelectable = TimeSpinnerClock.IsMinuteSelectable(hour, item.Value, meridiem, this.MinimumTime, this.MaximumTime);
                item.DisplayText = TimeSpinnerClock.FormatMinute(item.Value, culture, this.MinuteFormat);
            }
        }

        private void RebuildMeridiemItems()
        {
            var culture = this.EffectiveCulture;
            var existing = this.MeridiemItems;

            if (existing == null || existing.Count != 2)
            {
                this.MeridiemItems = new List<DateSpinnerItem>(2)
                {
                    this.CreateMeridiemItem(TimeSpinnerMeridiem.Am, culture),
                    this.CreateMeridiemItem(TimeSpinnerMeridiem.Pm, culture)
                };

                return;
            }

            foreach (var item in existing)
            {
                var meridiem = item.Value == (int)TimeSpinnerMeridiem.Pm ? TimeSpinnerMeridiem.Pm : TimeSpinnerMeridiem.Am;

                item.IsSelectable = TimeSpinnerClock.IsMeridiemSelectable(meridiem, this.MinimumTime, this.MaximumTime, this.MinuteInterval);
                item.DisplayText = TimeSpinnerClock.FormatMeridiem(meridiem, culture, this.MeridiemDisplayMode);
            }
        }

        private DateSpinnerItem CreateMeridiemItem(TimeSpinnerMeridiem meridiem, CultureInfo culture)
        {
            return new DateSpinnerItem(
                (int)meridiem,
                TimeSpinnerClock.FormatMeridiem(meridiem, culture, this.MeridiemDisplayMode),
                TimeSpinnerClock.IsMeridiemSelectable(meridiem, this.MinimumTime, this.MaximumTime, this.MinuteInterval));
        }

        /// <summary>
        /// Re-renders every wheel entry after a format or culture change without disturbing the collections
        /// themselves, so the wheels keep their scroll position.
        /// </summary>
        private void RefreshItemText()
        {
            var culture = this.EffectiveCulture;
            var meridiem = TimeSpinnerClock.ToMeridiem(_pendingTime);

            if (this.HourItems != null)
            {
                foreach (var item in this.HourItems)
                {
                    item.DisplayText = TimeSpinnerClock.FormatHour(item.Value, meridiem, culture, this.HourFormat);
                }
            }

            if (this.MinuteItems != null)
            {
                foreach (var item in this.MinuteItems)
                {
                    item.DisplayText = TimeSpinnerClock.FormatMinute(item.Value, culture, this.MinuteFormat);
                }
            }

            if (this.MeridiemItems != null)
            {
                foreach (var item in this.MeridiemItems)
                {
                    var value = item.Value == (int)TimeSpinnerMeridiem.Pm ? TimeSpinnerMeridiem.Pm : TimeSpinnerMeridiem.Am;
                    item.DisplayText = TimeSpinnerClock.FormatMeridiem(value, culture, this.MeridiemDisplayMode);
                }
            }
        }

        #endregion

        #region Presentation

        /// <summary>
        /// Collapses the minute column when the minute field is hidden, so the hour and AM/PM fields close the gap.
        /// </summary>
        private void RefreshColumnWidths()
        {
            this.MinuteColumnWidth = this.IsMinuteVisible ? new GridLength(1, GridUnitType.Star) : new GridLength(0);
        }

        /// <summary>
        /// Rebuilds the three strings shown on the closed entry surface.
        /// </summary>
        private void RefreshDisplayText()
        {
            var culture = this.EffectiveCulture;
            var time = this.SelectedTime;

            if (time.HasValue)
            {
                var meridiem = TimeSpinnerClock.ToMeridiem(time.Value);

                this.HourText = TimeSpinnerClock.FormatHour(TimeSpinnerClock.ToHour12(time.Value), meridiem, culture, this.HourFormat);
                this.MinuteText = TimeSpinnerClock.FormatMinute(time.Value.Minute, culture, this.MinuteFormat);
                this.MeridiemText = TimeSpinnerClock.FormatMeridiem(meridiem, culture, this.MeridiemDisplayMode);
                return;
            }

            this.HourText = this.ResolveText(this.HourPlaceholderText, HourPlaceholderTextKey, "Hour");
            this.MinuteText = this.ResolveText(this.MinutePlaceholderText, MinutePlaceholderTextKey, "Minute");
            this.MeridiemText = this.ResolveText(this.MeridiemPlaceholderText, MeridiemPlaceholderTextKey, "AM/PM");
        }

        /// <summary>
        /// Resolves a caption from the explicit property, then the theme resource, then a built in English default.
        /// The resource step is what lets an application localize every spinner from one merged dictionary.
        /// </summary>
        private string ResolveText(string? explicitValue, ComponentResourceKey key, string fallback)
        {
            if (!string.IsNullOrEmpty(explicitValue))
            {
                return explicitValue;
            }

            return this.TryFindResource(key) as string ?? fallback;
        }

        #endregion

        #region Input

        private void OnRootMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.ActivateFromEntrySurface(e.OriginalSource as DependencyObject))
            {
                e.Handled = true;
            }
        }

        private void OnRootTouchUp(object? sender, TouchEventArgs e)
        {
            if (this.ActivateFromEntrySurface(e.OriginalSource as DependencyObject))
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// Activates the entry surface after a mouse click or tap has completed. Waiting for the release prevents
        /// a light-dismiss popup from treating the opening press as an outside interaction.
        /// </summary>
        private bool ActivateFromEntrySurface(DependencyObject? source)
        {
            if (!this.CanInteract)
            {
                return false;
            }

            // The clear button lives on the entry surface and must not double as an open gesture.
            if (_clearButton != null && source != null && _clearButton.IsAncestorOf(source))
            {
                return false;
            }

            this.Focus();

            if (this.IsDropDownOpen)
            {
                this.Close();
            }
            else
            {
                this.Open();
            }

            return true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (e.Handled)
            {
                return;
            }

            if (this.IsDropDownOpen)
            {
                if (e.Key == Key.Escape)
                {
                    this.Cancel();
                    e.Handled = true;
                }

                return;
            }

            if (!this.CanInteract)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Enter:
                case Key.Space:
                    this.Open();
                    e.Handled = true;
                    break;
                case Key.Down when (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt:
                case Key.System when e.SystemKey == Key.Down && (Keyboard.Modifiers & ModifierKeys.Alt) == ModifierKeys.Alt:
                    this.Open();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Handles the keys that belong to the drop down as a whole rather than to a single wheel. Left and Right
        /// move between wheels, Enter commits, Escape cancels. Tab is deliberately left alone so focus is never
        /// trapped inside the popup.
        /// </summary>
        private void OnSelectorPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || sender is not DateSpinnerSelector selector)
            {
                return;
            }

            switch (e.Key)
            {
                case Key.Escape:
                    this.Cancel();
                    e.Handled = true;
                    break;
                case Key.Enter:
                    if (this.CommitMode == TimeSpinnerCommitMode.Immediate)
                    {
                        this.Close();
                    }
                    else
                    {
                        this.Apply();
                    }

                    e.Handled = true;
                    break;
                case Key.Left:
                    e.Handled = this.MoveFocusBetweenSelectors(selector, -1);
                    break;
                case Key.Right:
                    e.Handled = this.MoveFocusBetweenSelectors(selector, 1);
                    break;
            }
        }

        /// <summary>
        /// Moves keyboard focus to the neighbouring visible wheel, stopping at the ends rather than wrapping.
        /// </summary>
        private bool MoveFocusBetweenSelectors(DateSpinnerSelector current, int direction)
        {
            var selectors = this.VisibleSelectors();
            int index = selectors.IndexOf(current);

            if (index < 0)
            {
                return false;
            }

            int target = index + direction;

            if (target < 0 || target >= selectors.Count)
            {
                return false;
            }

            selectors[target].Focus();

            return true;
        }

        #endregion

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new TimeSpinnerAutomationPeer(this);
        }
    }
}
