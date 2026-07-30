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
    /// A date entry control that chooses a date with three coordinated scrolling wheels for month, day, and year
    /// rather than with a calendar grid. The closed control shows a compact entry surface with one field per date
    /// component; clicking it opens a drop down containing the wheels.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The interaction model follows the WinUI 3 date picker: fields appear in the order the current culture writes
    /// them, an unset control shows localized placeholders, and by default the drop down commits explicitly through
    /// Apply and Cancel buttons. Nothing about the WinUI visual design is reproduced, the control is templated
    /// entirely from Mosaic theme resources.
    /// </para>
    /// <para>
    /// <see cref="SelectedDate"/> is nullable and always normalized to midnight. While the drop down is open the
    /// control tracks a separate temporary date, so opening and closing without touching a wheel never assigns a
    /// value. See <see cref="CommitMode"/> for how the temporary date reaches <see cref="SelectedDate"/>.
    /// </para>
    /// <para>
    /// Values outside <see cref="MinimumDate"/> and <see cref="MaximumDate"/> are still rendered on the wheels but
    /// are not selectable, which keeps the wheels' geometry stable while the user moves across a boundary. An
    /// externally assigned <see cref="SelectedDate"/> is coerced into the range rather than rejected.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code language="xml">
    /// &lt;mosaic:DateSpinner Header="Arrival date"
    ///                     SelectedDate="{Binding ArrivalDate, Mode=TwoWay}"
    ///                     MinimumDate="{Binding EarliestArrivalDate}"
    ///                     MaximumDate="{Binding LatestArrivalDate}"
    ///                     IsClearButtonVisible="True" /&gt;
    /// </code>
    /// </example>
    [TemplatePart(Name = PartRoot, Type = typeof(FrameworkElement))]
    [TemplatePart(Name = PartPopup, Type = typeof(Popup))]
    [TemplatePart(Name = PartMonthSelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartDaySelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartYearSelector, Type = typeof(DateSpinnerSelector))]
    [TemplatePart(Name = PartApplyButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartCancelButton, Type = typeof(ButtonBase))]
    [TemplatePart(Name = PartClearButton, Type = typeof(ButtonBase))]
    [DefaultEvent(nameof(SelectedDateChanged))]
    [DefaultProperty(nameof(SelectedDate))]
    public class DateSpinner : Control
    {
        private const string PartRoot = "PART_Root";
        private const string PartPopup = "PART_Popup";
        private const string PartMonthSelector = "PART_MonthSelector";
        private const string PartDaySelector = "PART_DaySelector";
        private const string PartYearSelector = "PART_YearSelector";
        private const string PartApplyButton = "PART_ApplyButton";
        private const string PartCancelButton = "PART_CancelButton";
        private const string PartClearButton = "PART_ClearButton";

        /// <summary>
        /// How far either side of today the default range reaches.
        /// </summary>
        private const int DefaultRangeYears = 100;

        private FrameworkElement? _root;
        private Popup? _popup;
        private DateSpinnerSelector? _monthSelector;
        private DateSpinnerSelector? _daySelector;
        private DateSpinnerSelector? _yearSelector;
        private ButtonBase? _applyButton;
        private ButtonBase? _cancelButton;
        private ButtonBase? _clearButton;
        private Window? _hostWindow;

        /// <summary>
        /// The temporary date the wheels are editing while the drop down is open. Never written to
        /// <see cref="SelectedDate"/> unless the commit mode or the user asks for it.
        /// </summary>
        private DateTime _pendingDate = DateTime.Today;

        /// <summary>
        /// The value of <see cref="SelectedDate"/> at the moment the drop down opened, used by <see cref="Cancel"/>.
        /// </summary>
        private DateTime? _dateBeforeOpen;

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
        /// The resource key for the default month placeholder text. Override this key in a merged resource
        /// dictionary to localize the placeholder without setting <see cref="MonthPlaceholderText"/> on every
        /// instance.
        /// </summary>
        public static ComponentResourceKey MonthPlaceholderTextKey { get; } = new(typeof(DateSpinner), "MonthPlaceholderText");

        /// <summary>
        /// The resource key for the default day placeholder text.
        /// </summary>
        public static ComponentResourceKey DayPlaceholderTextKey { get; } = new(typeof(DateSpinner), "DayPlaceholderText");

        /// <summary>
        /// The resource key for the default year placeholder text.
        /// </summary>
        public static ComponentResourceKey YearPlaceholderTextKey { get; } = new(typeof(DateSpinner), "YearPlaceholderText");

        /// <summary>
        /// The resource key for the Apply button's caption.
        /// </summary>
        public static ComponentResourceKey ApplyButtonTextKey { get; } = new(typeof(DateSpinner), "ApplyButtonText");

        /// <summary>
        /// The resource key for the Cancel button's caption.
        /// </summary>
        public static ComponentResourceKey CancelButtonTextKey { get; } = new(typeof(DateSpinner), "CancelButtonText");

        /// <summary>
        /// The resource key for the Clear button's accessible name and tooltip.
        /// </summary>
        public static ComponentResourceKey ClearButtonTextKey { get; } = new(typeof(DateSpinner), "ClearButtonText");

        #endregion

        #region Routed Commands

        /// <summary>
        /// Opens the drop down. Bound to the entry surface by the default template.
        /// </summary>
        public static RoutedUICommand OpenCommand { get; } = new("Open", nameof(OpenCommand), typeof(DateSpinner));

        /// <summary>
        /// Closes the drop down without committing the temporary date.
        /// </summary>
        public static RoutedUICommand CloseCommand { get; } = new("Close", nameof(CloseCommand), typeof(DateSpinner));

        /// <summary>
        /// Clears <see cref="SelectedDate"/>. Bound to the clear button by the default template.
        /// </summary>
        public static RoutedUICommand ClearCommand { get; } = new("Clear", nameof(ClearCommand), typeof(DateSpinner));

        /// <summary>
        /// Commits the temporary date and closes the drop down. Bound to the Apply button by the default template.
        /// </summary>
        public static RoutedUICommand ApplyCommand { get; } = new("Apply", nameof(ApplyCommand), typeof(DateSpinner));

        /// <summary>
        /// Restores the date that was selected when the drop down opened and closes it. Bound to the Cancel button
        /// by the default template.
        /// </summary>
        public static RoutedUICommand CancelCommand { get; } = new("Cancel", nameof(CancelCommand), typeof(DateSpinner));

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SelectedDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedDateProperty = DependencyProperty.Register(
            nameof(SelectedDate), typeof(DateTime?), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged, CoerceSelectedDate));

        /// <summary>
        /// The selected date, or <see langword="null"/> when the control is unset. Always normalized to midnight
        /// and always coerced into <see cref="MinimumDate"/> to <see cref="MaximumDate"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("The selected date, or null when nothing is selected.")]
        public DateTime? SelectedDate
        {
            get => (DateTime?)this.GetValue(SelectedDateProperty);
            set => this.SetValue(SelectedDateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MinimumDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MinimumDateProperty = DependencyProperty.Register(
            nameof(MinimumDate), typeof(DateTime), typeof(DateSpinner),
            new FrameworkPropertyMetadata(DateTime.Today.AddYears(-DefaultRangeYears), OnRangeChanged, CoerceDateOnly));

        /// <summary>
        /// The earliest selectable date, inclusive. Defaults to one hundred years before today.
        /// </summary>
        [Category("Mosaic")]
        [Description("The earliest selectable date, inclusive.")]
        public DateTime MinimumDate
        {
            get => (DateTime)this.GetValue(MinimumDateProperty);
            set => this.SetValue(MinimumDateProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MaximumDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MaximumDateProperty = DependencyProperty.Register(
            nameof(MaximumDate), typeof(DateTime), typeof(DateSpinner),
            new FrameworkPropertyMetadata(DateTime.Today.AddYears(DefaultRangeYears), OnRangeChanged, CoerceMaximumDate));

        /// <summary>
        /// The latest selectable date, inclusive. Defaults to one hundred years after today. An assignment earlier
        /// than <see cref="MinimumDate"/> is coerced up to it rather than throwing, so the invariant
        /// <c>MinimumDate &lt;= MaximumDate</c> always holds.
        /// </summary>
        [Category("Mosaic")]
        [Description("The latest selectable date, inclusive. Coerced up to MinimumDate when set below it.")]
        public DateTime MaximumDate
        {
            get => (DateTime)this.GetValue(MaximumDateProperty);
            set => this.SetValue(MaximumDateProperty, value);
        }

        private static readonly DependencyPropertyKey IsDropDownOpenPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsDropDownOpen), typeof(bool), typeof(DateSpinner), new FrameworkPropertyMetadata(false));

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
            nameof(CommitMode), typeof(DateSpinnerCommitMode), typeof(DateSpinner),
            new FrameworkPropertyMetadata(DateSpinnerCommitMode.Explicit));

        /// <summary>
        /// When edits made on the wheels are written back to <see cref="SelectedDate"/>. Defaults to
        /// <see cref="DateSpinnerCommitMode.Explicit"/>, matching the accept/dismiss model of the WinUI 3 picker.
        /// </summary>
        [Category("Mosaic")]
        [Description("When wheel edits are written back to SelectedDate.")]
        public DateSpinnerCommitMode CommitMode
        {
            get => (DateSpinnerCommitMode)this.GetValue(CommitModeProperty);
            set => this.SetValue(CommitModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="LightDismissBehavior"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LightDismissBehaviorProperty = DependencyProperty.Register(
            nameof(LightDismissBehavior), typeof(DateSpinnerLightDismissBehavior), typeof(DateSpinner),
            new FrameworkPropertyMetadata(DateSpinnerLightDismissBehavior.Cancel));

        /// <summary>
        /// What clicking outside an open drop down does with the temporary date. Defaults to
        /// <see cref="DateSpinnerLightDismissBehavior.Cancel"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("What clicking outside an open drop down does with the temporary date.")]
        public DateSpinnerLightDismissBehavior LightDismissBehavior
        {
            get => (DateSpinnerLightDismissBehavior)this.GetValue(LightDismissBehaviorProperty);
            set => this.SetValue(LightDismissBehaviorProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsMonthVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMonthVisibleProperty = DependencyProperty.Register(
            nameof(IsMonthVisible), typeof(bool), typeof(DateSpinner),
            new FrameworkPropertyMetadata(true, OnFieldVisibilityChanged, CoerceIsMonthVisible));

        /// <summary>
        /// Whether the month field is shown. At least one field must remain visible, so an attempt to hide the last
        /// visible field is coerced back to <see langword="true"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the month field is shown.")]
        public bool IsMonthVisible
        {
            get => (bool)this.GetValue(IsMonthVisibleProperty);
            set => this.SetValue(IsMonthVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsDayVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsDayVisibleProperty = DependencyProperty.Register(
            nameof(IsDayVisible), typeof(bool), typeof(DateSpinner),
            new FrameworkPropertyMetadata(true, OnFieldVisibilityChanged, CoerceIsDayVisible));

        /// <summary>
        /// Whether the day field is shown. Hiding it preserves the day component of the selected or temporary date.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the day field is shown.")]
        public bool IsDayVisible
        {
            get => (bool)this.GetValue(IsDayVisibleProperty);
            set => this.SetValue(IsDayVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsYearVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsYearVisibleProperty = DependencyProperty.Register(
            nameof(IsYearVisible), typeof(bool), typeof(DateSpinner),
            new FrameworkPropertyMetadata(true, OnFieldVisibilityChanged, CoerceIsYearVisible));

        /// <summary>
        /// Whether the year field is shown. Hiding it preserves the year component of the selected or temporary date.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the year field is shown.")]
        public bool IsYearVisible
        {
            get => (bool)this.GetValue(IsYearVisibleProperty);
            set => this.SetValue(IsYearVisibleProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MonthDisplayMode"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthDisplayModeProperty = DependencyProperty.Register(
            nameof(MonthDisplayMode), typeof(DateSpinnerMonthDisplayMode), typeof(DateSpinner),
            new FrameworkPropertyMetadata(DateSpinnerMonthDisplayMode.FullName, OnFormatChanged));

        /// <summary>
        /// How months are rendered when <see cref="MonthFormat"/> has not been set. Defaults to the culture's full
        /// month name.
        /// </summary>
        [Category("Mosaic")]
        [Description("How months are rendered when MonthFormat is not set.")]
        public DateSpinnerMonthDisplayMode MonthDisplayMode
        {
            get => (DateSpinnerMonthDisplayMode)this.GetValue(MonthDisplayModeProperty);
            set => this.SetValue(MonthDisplayModeProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="MonthFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthFormatProperty = DependencyProperty.Register(
            nameof(MonthFormat), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// An optional .NET date format string applied to month values, for example <c>MMM</c>. Overrides
        /// <see cref="MonthDisplayMode"/>. A format string the runtime cannot honor falls back to the full month
        /// name rather than throwing.
        /// </summary>
        [Category("Mosaic")]
        [Description("An optional .NET date format string applied to month values.")]
        public string? MonthFormat
        {
            get => (string?)this.GetValue(MonthFormatProperty);
            set => this.SetValue(MonthFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DayFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayFormatProperty = DependencyProperty.Register(
            nameof(DayFormat), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// An optional .NET date format string applied to day values, for example <c>dd</c>. Defaults to the
        /// localized number with no padding.
        /// </summary>
        [Category("Mosaic")]
        [Description("An optional .NET date format string applied to day values.")]
        public string? DayFormat
        {
            get => (string?)this.GetValue(DayFormatProperty);
            set => this.SetValue(DayFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="YearFormat"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearFormatProperty = DependencyProperty.Register(
            nameof(YearFormat), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// An optional .NET date format string applied to year values, for example <c>yy</c>. Defaults to a four
        /// digit year.
        /// </summary>
        [Category("Mosaic")]
        [Description("An optional .NET date format string applied to year values.")]
        public string? YearFormat
        {
            get => (string?)this.GetValue(YearFormatProperty);
            set => this.SetValue(YearFormatProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Culture"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CultureProperty = DependencyProperty.Register(
            nameof(Culture), typeof(CultureInfo), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnCultureChanged));

        /// <summary>
        /// The culture supplying month names, number formatting, and the order the fields are laid out in.
        /// Defaults to <see cref="CultureInfo.CurrentUICulture"/> when <see langword="null"/>.
        /// </summary>
        [Category("Mosaic")]
        [Description("The culture supplying month names, formatting, and field order.")]
        public CultureInfo? Culture
        {
            get => (CultureInfo?)this.GetValue(CultureProperty);
            set => this.SetValue(CultureProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="Header"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HeaderProperty = DependencyProperty.Register(
            nameof(Header), typeof(object), typeof(DateSpinner), new FrameworkPropertyMetadata(null));

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
            nameof(HeaderTemplate), typeof(DataTemplate), typeof(DateSpinner), new FrameworkPropertyMetadata(null));

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
        /// Identifies the <see cref="MonthPlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthPlaceholderTextProperty = DependencyProperty.Register(
            nameof(MonthPlaceholderText), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the month field when nothing is selected. Falls back to the
        /// <see cref="MonthPlaceholderTextKey"/> resource so it can be localized centrally.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the month field when nothing is selected.")]
        public string? MonthPlaceholderText
        {
            get => (string?)this.GetValue(MonthPlaceholderTextProperty);
            set => this.SetValue(MonthPlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="DayPlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayPlaceholderTextProperty = DependencyProperty.Register(
            nameof(DayPlaceholderText), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the day field when nothing is selected. Falls back to the
        /// <see cref="DayPlaceholderTextKey"/> resource.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the day field when nothing is selected.")]
        public string? DayPlaceholderText
        {
            get => (string?)this.GetValue(DayPlaceholderTextProperty);
            set => this.SetValue(DayPlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="YearPlaceholderText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearPlaceholderTextProperty = DependencyProperty.Register(
            nameof(YearPlaceholderText), typeof(string), typeof(DateSpinner),
            new FrameworkPropertyMetadata(null, OnFormatChanged));

        /// <summary>
        /// The text shown in the year field when nothing is selected. Falls back to the
        /// <see cref="YearPlaceholderTextKey"/> resource.
        /// </summary>
        [Category("Mosaic")]
        [Description("The text shown in the year field when nothing is selected.")]
        public string? YearPlaceholderText
        {
            get => (string?)this.GetValue(YearPlaceholderTextProperty);
            set => this.SetValue(YearPlaceholderTextProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="IsClearButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClearButtonVisibleProperty = DependencyProperty.Register(
            nameof(IsClearButtonVisible), typeof(bool), typeof(DateSpinner), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Whether the entry surface shows a clear button. The button is only enabled while a date is selected and
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
            nameof(IsReadOnly), typeof(bool), typeof(DateSpinner),
            new FrameworkPropertyMetadata(false, OnIsReadOnlyChanged));

        /// <summary>
        /// Whether the control is read only. The selected date stays visible and the control stays focusable, but
        /// the drop down cannot be opened and the value cannot be changed or cleared. Setting this while the drop
        /// down is open closes it.
        /// </summary>
        [Category("Mosaic")]
        [Description("Whether the value can be changed. A read only control still shows its date and takes focus.")]
        public bool IsReadOnly
        {
            get => (bool)this.GetValue(IsReadOnlyProperty);
            set => this.SetValue(IsReadOnlyProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="ItemHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ItemHeightProperty = DependencyProperty.Register(
            nameof(ItemHeight), typeof(double), typeof(DateSpinner), new FrameworkPropertyMetadata(30.0));

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
            nameof(VisibleItemCount), typeof(int), typeof(DateSpinner), new FrameworkPropertyMetadata(5));

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
            nameof(IsScrollAnimationEnabled), typeof(bool), typeof(DateSpinner), new FrameworkPropertyMetadata(true));

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

        private static readonly DependencyPropertyKey MonthItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MonthItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(DateSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="MonthItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthItemsProperty = MonthItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The entries offered by the month wheel. Rebuilt only when the set of months changes; a change that only
        /// affects which months are selectable mutates the existing entries in place.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? MonthItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(MonthItemsProperty);
            private set => this.SetValue(MonthItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DayItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DayItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(DateSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="DayItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayItemsProperty = DayItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The entries offered by the day wheel, sized to the length of the temporary month.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? DayItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(DayItemsProperty);
            private set => this.SetValue(DayItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey YearItemsPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(YearItems), typeof(IReadOnlyList<DateSpinnerItem>), typeof(DateSpinner), new FrameworkPropertyMetadata(null));

        /// <summary>
        /// Identifies the <see cref="YearItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearItemsProperty = YearItemsPropertyKey.DependencyProperty;

        /// <summary>
        /// The entries offered by the year wheel, covering <see cref="MinimumDate"/> to <see cref="MaximumDate"/>.
        /// The wheel virtualizes, so a wide range does not create a container per year.
        /// </summary>
        [Browsable(false)]
        public IReadOnlyList<DateSpinnerItem>? YearItems
        {
            get => (IReadOnlyList<DateSpinnerItem>?)this.GetValue(YearItemsProperty);
            private set => this.SetValue(YearItemsPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MonthTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MonthText), typeof(string), typeof(DateSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="MonthText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthTextProperty = MonthTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The month field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string MonthText
        {
            get => (string)this.GetValue(MonthTextProperty);
            private set => this.SetValue(MonthTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DayTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DayText), typeof(string), typeof(DateSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DayText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayTextProperty = DayTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The day field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string DayText
        {
            get => (string)this.GetValue(DayTextProperty);
            private set => this.SetValue(DayTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey YearTextPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(YearText), typeof(string), typeof(DateSpinner), new FrameworkPropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="YearText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearTextProperty = YearTextPropertyKey.DependencyProperty;

        /// <summary>
        /// The year field's text on the closed entry surface, or its placeholder when nothing is selected.
        /// </summary>
        [Browsable(false)]
        public string YearText
        {
            get => (string)this.GetValue(YearTextProperty);
            private set => this.SetValue(YearTextPropertyKey, value);
        }

        private static readonly DependencyPropertyKey HasSelectedDatePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasSelectedDate), typeof(bool), typeof(DateSpinner), new FrameworkPropertyMetadata(false));

        /// <summary>
        /// Identifies the <see cref="HasSelectedDate"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasSelectedDateProperty = HasSelectedDatePropertyKey.DependencyProperty;

        /// <summary>
        /// Whether a date is currently selected. The template uses this to render placeholders differently from
        /// real values, so the unset state does not rely on color alone.
        /// </summary>
        [Browsable(false)]
        public bool HasSelectedDate
        {
            get => (bool)this.GetValue(HasSelectedDateProperty);
            private set => this.SetValue(HasSelectedDatePropertyKey, value);
        }

        private static readonly DependencyPropertyKey MonthFieldIndexPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MonthFieldIndex), typeof(int), typeof(DateSpinner), new FrameworkPropertyMetadata(0));

        /// <summary>
        /// Identifies the <see cref="MonthFieldIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthFieldIndexProperty = MonthFieldIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// The month field's position, zero based, in the culture's field order. The template binds it to a grid
        /// column so the layout follows the culture without any logic in the template.
        /// </summary>
        [Browsable(false)]
        public int MonthFieldIndex
        {
            get => (int)this.GetValue(MonthFieldIndexProperty);
            private set => this.SetValue(MonthFieldIndexPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DayFieldIndexPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DayFieldIndex), typeof(int), typeof(DateSpinner), new FrameworkPropertyMetadata(1));

        /// <summary>
        /// Identifies the <see cref="DayFieldIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DayFieldIndexProperty = DayFieldIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// The day field's position, zero based, in the culture's field order.
        /// </summary>
        [Browsable(false)]
        public int DayFieldIndex
        {
            get => (int)this.GetValue(DayFieldIndexProperty);
            private set => this.SetValue(DayFieldIndexPropertyKey, value);
        }

        private static readonly DependencyPropertyKey YearFieldIndexPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(YearFieldIndex), typeof(int), typeof(DateSpinner), new FrameworkPropertyMetadata(2));

        /// <summary>
        /// Identifies the <see cref="YearFieldIndex"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearFieldIndexProperty = YearFieldIndexPropertyKey.DependencyProperty;

        /// <summary>
        /// The year field's position, zero based, in the culture's field order.
        /// </summary>
        [Browsable(false)]
        public int YearFieldIndex
        {
            get => (int)this.GetValue(YearFieldIndexProperty);
            private set => this.SetValue(YearFieldIndexPropertyKey, value);
        }

        private static readonly DependencyPropertyKey FirstFieldColumnWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(FirstFieldColumnWidth), typeof(GridLength), typeof(DateSpinner),
            new FrameworkPropertyMetadata(new GridLength(1, GridUnitType.Star)));

        /// <summary>
        /// Identifies the <see cref="FirstFieldColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty FirstFieldColumnWidthProperty = FirstFieldColumnWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// The width of the first culture-ordered field column. The template collapses it when its field is hidden.
        /// </summary>
        [Browsable(false)]
        public GridLength FirstFieldColumnWidth
        {
            get => (GridLength)this.GetValue(FirstFieldColumnWidthProperty);
            private set => this.SetValue(FirstFieldColumnWidthPropertyKey, value);
        }

        private static readonly DependencyPropertyKey SecondFieldColumnWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(SecondFieldColumnWidth), typeof(GridLength), typeof(DateSpinner),
            new FrameworkPropertyMetadata(new GridLength(1, GridUnitType.Star)));

        /// <summary>
        /// Identifies the <see cref="SecondFieldColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SecondFieldColumnWidthProperty = SecondFieldColumnWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// The width of the second culture-ordered field column. The template collapses it when its field is hidden.
        /// </summary>
        [Browsable(false)]
        public GridLength SecondFieldColumnWidth
        {
            get => (GridLength)this.GetValue(SecondFieldColumnWidthProperty);
            private set => this.SetValue(SecondFieldColumnWidthPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ThirdFieldColumnWidthPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(ThirdFieldColumnWidth), typeof(GridLength), typeof(DateSpinner),
            new FrameworkPropertyMetadata(new GridLength(1, GridUnitType.Star)));

        /// <summary>
        /// Identifies the <see cref="ThirdFieldColumnWidth"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ThirdFieldColumnWidthProperty = ThirdFieldColumnWidthPropertyKey.DependencyProperty;

        /// <summary>
        /// The width of the third culture-ordered field column. The template collapses it when its field is hidden.
        /// </summary>
        [Browsable(false)]
        public GridLength ThirdFieldColumnWidth
        {
            get => (GridLength)this.GetValue(ThirdFieldColumnWidthProperty);
            private set => this.SetValue(ThirdFieldColumnWidthPropertyKey, value);
        }

        private static readonly DependencyPropertyKey MonthSeparatorVisibilityPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(MonthSeparatorVisibility), typeof(Visibility), typeof(DateSpinner), new FrameworkPropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Identifies the <see cref="MonthSeparatorVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MonthSeparatorVisibilityProperty = MonthSeparatorVisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether the divider drawn to the left of the month field is shown. Collapsed when the month is the first
        /// visible field, so the entry surface reads as one control rather than three.
        /// </summary>
        [Browsable(false)]
        public Visibility MonthSeparatorVisibility
        {
            get => (Visibility)this.GetValue(MonthSeparatorVisibilityProperty);
            private set => this.SetValue(MonthSeparatorVisibilityPropertyKey, value);
        }

        private static readonly DependencyPropertyKey DaySeparatorVisibilityPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(DaySeparatorVisibility), typeof(Visibility), typeof(DateSpinner), new FrameworkPropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Identifies the <see cref="DaySeparatorVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DaySeparatorVisibilityProperty = DaySeparatorVisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether the divider drawn to the left of the day field is shown.
        /// </summary>
        [Browsable(false)]
        public Visibility DaySeparatorVisibility
        {
            get => (Visibility)this.GetValue(DaySeparatorVisibilityProperty);
            private set => this.SetValue(DaySeparatorVisibilityPropertyKey, value);
        }

        private static readonly DependencyPropertyKey YearSeparatorVisibilityPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(YearSeparatorVisibility), typeof(Visibility), typeof(DateSpinner), new FrameworkPropertyMetadata(Visibility.Collapsed));

        /// <summary>
        /// Identifies the <see cref="YearSeparatorVisibility"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YearSeparatorVisibilityProperty = YearSeparatorVisibilityPropertyKey.DependencyProperty;

        /// <summary>
        /// Whether the divider drawn to the left of the year field is shown.
        /// </summary>
        [Browsable(false)]
        public Visibility YearSeparatorVisibility
        {
            get => (Visibility)this.GetValue(YearSeparatorVisibilityProperty);
            private set => this.SetValue(YearSeparatorVisibilityPropertyKey, value);
        }

        #endregion

        #region Routed Events

        /// <summary>
        /// Identifies the <see cref="SelectedDateChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent SelectedDateChangedEvent = EventManager.RegisterRoutedEvent(
            nameof(SelectedDateChanged), RoutingStrategy.Bubble, typeof(EventHandler<DateSpinnerDateChangedEventArgs>), typeof(DateSpinner));

        /// <summary>
        /// Occurs when <see cref="SelectedDate"/> changes to a different calendar day. Assigning a value that
        /// normalizes or coerces to the day already selected raises nothing.
        /// </summary>
        public event EventHandler<DateSpinnerDateChangedEventArgs> SelectedDateChanged
        {
            add => this.AddHandler(SelectedDateChangedEvent, value);
            remove => this.RemoveHandler(SelectedDateChangedEvent, value);
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent DropDownOpenedEvent = EventManager.RegisterRoutedEvent(
            nameof(DropDownOpened), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DateSpinner));

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
            nameof(DropDownClosed), RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(DateSpinner));

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
        /// Initializes static metadata for the <see cref="DateSpinner"/> class.
        /// </summary>
        static DateSpinner()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(DateSpinner), new FrameworkPropertyMetadata(typeof(DateSpinner)));
            FocusableProperty.OverrideMetadata(typeof(DateSpinner), new FrameworkPropertyMetadata(true));
            KeyboardNavigation.IsTabStopProperty.OverrideMetadata(typeof(DateSpinner), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DateSpinner"/> class.
        /// </summary>
        public DateSpinner()
        {
            this.CommandBindings.Add(new CommandBinding(OpenCommand, (_, e) => { this.Open(); e.Handled = true; }, (_, e) => e.CanExecute = this.CanInteract));
            this.CommandBindings.Add(new CommandBinding(CloseCommand, (_, e) => { this.Close(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));
            this.CommandBindings.Add(new CommandBinding(ClearCommand, (_, e) => { this.Clear(); e.Handled = true; }, (_, e) => e.CanExecute = this.CanInteract && this.SelectedDate.HasValue));
            this.CommandBindings.Add(new CommandBinding(ApplyCommand, (_, e) => { this.Apply(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));
            this.CommandBindings.Add(new CommandBinding(CancelCommand, (_, e) => { this.Cancel(); e.Handled = true; }, (_, e) => e.CanExecute = this.IsDropDownOpen));

            this.Loaded += this.OnSpinnerLoaded;
            this.Unloaded += this.OnSpinnerUnloaded;

            this.RefreshFieldOrder();
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
            _monthSelector = this.GetTemplateChild(PartMonthSelector) as DateSpinnerSelector;
            _daySelector = this.GetTemplateChild(PartDaySelector) as DateSpinnerSelector;
            _yearSelector = this.GetTemplateChild(PartYearSelector) as DateSpinnerSelector;
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

            _monthSelector = null;
            _daySelector = null;
            _yearSelector = null;
            _applyButton = null;
            _cancelButton = null;
            _clearButton = null;
        }

        /// <summary>
        /// The wheels that exist in the current template, in culture order, skipping hidden fields.
        /// </summary>
        private IEnumerable<DateSpinnerSelector> Selectors()
        {
            foreach (var field in DateSpinnerCalendar.GetFieldOrder(this.EffectiveCulture))
            {
                var selector = field switch
                {
                    DateSpinnerField.Month => _monthSelector,
                    DateSpinnerField.Day => _daySelector,
                    _ => _yearSelector
                };

                if (selector != null)
                {
                    yield return selector;
                }
            }
        }

        /// <summary>
        /// The wheels that are currently visible, in culture order. Used for Left and Right navigation.
        /// </summary>
        private List<DateSpinnerSelector> VisibleSelectors()
        {
            var result = new List<DateSpinnerSelector>(3);

            foreach (var field in DateSpinnerCalendar.GetFieldOrder(this.EffectiveCulture))
            {
                switch (field)
                {
                    case DateSpinnerField.Month when this.IsMonthVisible && _monthSelector != null:
                        result.Add(_monthSelector);
                        break;
                    case DateSpinnerField.Day when this.IsDayVisible && _daySelector != null:
                        result.Add(_daySelector);
                        break;
                    case DateSpinnerField.Year when this.IsYearVisible && _yearSelector != null:
                        result.Add(_yearSelector);
                        break;
                }
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
        /// The temporary date starts at <see cref="SelectedDate"/>, or at today clamped into the permitted range
        /// when nothing is selected. Opening never assigns <see cref="SelectedDate"/>.
        /// </remarks>
        public void Open()
        {
            if (this.IsDropDownOpen || !this.CanInteract)
            {
                return;
            }

            _dateBeforeOpen = this.SelectedDate;
            _pendingDate = this.SelectedDate ?? DateSpinnerCalendar.Clamp(DateTime.Today, this.MinimumDate, this.MaximumDate);

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
        /// Closes the wheel drop down without committing the temporary date. In
        /// <see cref="DateSpinnerCommitMode.Explicit"/> mode the temporary date is discarded; in
        /// <see cref="DateSpinnerCommitMode.Immediate"/> mode the value has already been written through and is kept.
        /// </summary>
        public void Close()
        {
            this.CloseDropDown();
        }

        /// <summary>
        /// Commits the temporary date to <see cref="SelectedDate"/> and closes the drop down. Does nothing when
        /// the drop down is not open.
        /// </summary>
        public void Apply()
        {
            if (!this.IsDropDownOpen)
            {
                return;
            }

            this.SetSelectedDate(_pendingDate);
            this.CloseDropDown();
        }

        /// <summary>
        /// Restores the date that was selected when the drop down opened and closes it. Does nothing when the drop
        /// down is not open.
        /// </summary>
        /// <remarks>
        /// This also undoes changes already written through in <see cref="DateSpinnerCommitMode.Immediate"/> mode,
        /// which is why the mode's own light dismiss path calls <see cref="Close"/> rather than this.
        /// </remarks>
        public void Cancel()
        {
            if (!this.IsDropDownOpen)
            {
                return;
            }

            this.SetSelectedDate(_dateBeforeOpen);
            this.CloseDropDown();
        }

        /// <summary>
        /// Clears the selection by setting <see cref="SelectedDate"/> to <see langword="null"/>. Does nothing when
        /// the control is read only or disabled.
        /// </summary>
        public void Clear()
        {
            if (!this.CanInteract)
            {
                return;
            }

            this.SetSelectedDate(null);
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
            if (this.CommitMode == DateSpinnerCommitMode.Explicit && this.LightDismissBehavior == DateSpinnerLightDismissBehavior.Apply)
            {
                this.SetSelectedDate(_pendingDate);
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

        private static object CoerceDateOnly(DependencyObject d, object baseValue)
        {
            return DateSpinnerCalendar.Normalize((DateTime)baseValue);
        }

        private static object CoerceMaximumDate(DependencyObject d, object baseValue)
        {
            var spinner = (DateSpinner)d;
            var value = DateSpinnerCalendar.Normalize((DateTime)baseValue);
            var minimum = spinner.MinimumDate;

            // The range invariant is maintained by lifting the maximum rather than by throwing, so a view model
            // that publishes both bounds can update them in either order.
            return value < minimum ? minimum : value;
        }

        private static object CoerceSelectedDate(DependencyObject d, object? baseValue)
        {
            if (baseValue is not DateTime value)
            {
                return null!;
            }

            var spinner = (DateSpinner)d;

            return DateSpinnerCalendar.Clamp(value, spinner.MinimumDate, spinner.MaximumDate);
        }

        private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;
            var oldDate = (DateTime?)e.OldValue;
            var newDate = (DateTime?)e.NewValue;

            spinner.HasSelectedDate = newDate.HasValue;
            spinner.RefreshDisplayText();

            // While the drop down is open the wheels are the source of truth, so an external assignment only
            // reshapes them when it did not originate there.
            if (!spinner._isSynchronizing && newDate.HasValue)
            {
                spinner._pendingDate = newDate.Value;
                spinner.RebuildAllItems();
                spinner.SyncSelectorsToPending();
            }

            spinner.RaiseEvent(new DateSpinnerDateChangedEventArgs(SelectedDateChangedEvent, spinner, oldDate, newDate));

            if (UIElementAutomationPeer.FromElement(spinner) is DateSpinnerAutomationPeer peer)
            {
                peer.RaiseValueChanged(oldDate, newDate);
            }

            CommandManager.InvalidateRequerySuggested();
        }

        private static void OnRangeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;

            // A new minimum can invalidate the maximum, and both can invalidate the selection.
            spinner.CoerceValue(MaximumDateProperty);
            spinner.CoerceValue(SelectedDateProperty);

            spinner._pendingDate = DateSpinnerCalendar.Clamp(spinner._pendingDate, spinner.MinimumDate, spinner.MaximumDate);
            spinner.RebuildAllItems();
            spinner.SyncSelectorsToPending();
            spinner.RefreshDisplayText();
        }

        /// <summary>
        /// Rejects an attempt to hide the month when it is the only field left. Each field gets its own callback
        /// because a coercion callback is not told which property it is running for.
        /// </summary>
        private static object CoerceIsMonthVisible(DependencyObject d, object baseValue)
        {
            var spinner = (DateSpinner)d;

            return (bool)baseValue || spinner.IsDayVisible || spinner.IsYearVisible ? baseValue : true;
        }

        /// <summary>
        /// Rejects an attempt to hide the day when it is the only field left.
        /// </summary>
        private static object CoerceIsDayVisible(DependencyObject d, object baseValue)
        {
            var spinner = (DateSpinner)d;

            return (bool)baseValue || spinner.IsMonthVisible || spinner.IsYearVisible ? baseValue : true;
        }

        /// <summary>
        /// Rejects an attempt to hide the year when it is the only field left.
        /// </summary>
        private static object CoerceIsYearVisible(DependencyObject d, object baseValue)
        {
            var spinner = (DateSpinner)d;

            return (bool)baseValue || spinner.IsMonthVisible || spinner.IsDayVisible ? baseValue : true;
        }

        private static void OnFieldVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;

            spinner.RefreshFieldOrder();
            spinner.RefreshDisplayText();
        }

        private static void OnFormatChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;

            spinner.RefreshItemText();
            spinner.RefreshDisplayText();
        }

        private static void OnCultureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;

            spinner.RefreshFieldOrder();
            spinner.RefreshItemText();
            spinner.RefreshDisplayText();
        }

        private static void OnIsReadOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var spinner = (DateSpinner)d;

            if (spinner.IsReadOnly && spinner.IsDropDownOpen)
            {
                spinner.Close();
            }

            CommandManager.InvalidateRequerySuggested();
        }

        #endregion

        #region Selection

        /// <summary>
        /// Writes a date to <see cref="SelectedDate"/>, normalizing it first and suppressing the write entirely
        /// when the effective calendar day has not changed. This is what keeps duplicate
        /// <see cref="SelectedDateChanged"/> notifications from reaching consumers.
        /// </summary>
        private void SetSelectedDate(DateTime? value)
        {
            var normalized = value.HasValue
                ? DateSpinnerCalendar.Clamp(value.Value, this.MinimumDate, this.MaximumDate)
                : (DateTime?)null;

            if (Nullable.Equals(this.SelectedDate, normalized))
            {
                return;
            }

            _isSynchronizing = true;

            try
            {
                this.SelectedDate = normalized;
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

            int year = _pendingDate.Year;
            int month = _pendingDate.Month;
            int day = _pendingDate.Day;

            if (ReferenceEquals(selector, _yearSelector) && selector.SelectedNumber.HasValue)
            {
                year = selector.SelectedNumber.Value;
            }
            else if (ReferenceEquals(selector, _monthSelector) && selector.SelectedNumber.HasValue)
            {
                month = selector.SelectedNumber.Value;
            }
            else if (ReferenceEquals(selector, _daySelector) && selector.SelectedNumber.HasValue)
            {
                day = selector.SelectedNumber.Value;
            }
            else
            {
                return;
            }

            // Compose clamps the day to the length of the target month, so January 31 moving to February lands on
            // the 28th or 29th instead of producing an invalid date part way through the update.
            var candidate = DateSpinnerCalendar.Compose(year, month, day);
            _pendingDate = DateSpinnerCalendar.Clamp(candidate, this.MinimumDate, this.MaximumDate);

            this.RebuildAllItems();
            this.SyncSelectorsToPending();

            if (this.CommitMode == DateSpinnerCommitMode.Immediate)
            {
                this.SetSelectedDate(_pendingDate);
            }
        }

        /// <summary>
        /// Pushes the temporary date onto the wheels without letting the resulting change notifications feed back
        /// into <see cref="OnSelectorValueChanged"/>.
        /// </summary>
        private void SyncSelectorsToPending()
        {
            _isSynchronizing = true;

            try
            {
                if (_monthSelector != null)
                {
                    _monthSelector.SelectedNumber = _pendingDate.Month;
                }

                if (_daySelector != null)
                {
                    _daySelector.SelectedNumber = _pendingDate.Day;
                }

                if (_yearSelector != null)
                {
                    _yearSelector.SelectedNumber = _pendingDate.Year;
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
        /// Rebuilds the three wheels for the current temporary date and range. The month and year collections are
        /// only re-created when their shape changes, otherwise the existing entries are updated in place so the
        /// wheels do not lose their realized containers on every keystroke.
        /// </summary>
        private void RebuildAllItems()
        {
            this.RebuildYearItems();
            this.RebuildMonthItems();
            this.RebuildDayItems();
        }

        private void RebuildYearItems()
        {
            var culture = this.EffectiveCulture;
            int first = this.MinimumDate.Year;
            int last = this.MaximumDate.Year;

            var existing = this.YearItems;

            if (existing != null && existing.Count == last - first + 1 && existing.Count > 0 && existing[0].Value == first)
            {
                // Same span, every year in it is selectable by definition, nothing to do beyond text.
                return;
            }

            var items = new List<DateSpinnerItem>(last - first + 1);

            for (int year = first; year <= last; year++)
            {
                items.Add(new DateSpinnerItem(year, DateSpinnerCalendar.FormatYear(year, culture, this.YearFormat), true));
            }

            this.YearItems = items;
        }

        private void RebuildMonthItems()
        {
            var culture = this.EffectiveCulture;
            var (first, last) = DateSpinnerCalendar.GetMonthRange(_pendingDate.Year, this.MinimumDate, this.MaximumDate);
            var existing = this.MonthItems;

            // All twelve months are always present; only which of them can be chosen changes with the year.
            if (existing == null || existing.Count != 12)
            {
                var items = new List<DateSpinnerItem>(12);

                for (int month = 1; month <= 12; month++)
                {
                    string text = DateSpinnerCalendar.FormatMonth(_pendingDate.Year, month, culture, this.MonthDisplayMode, this.MonthFormat);
                    items.Add(new DateSpinnerItem(month, text, month >= first && month <= last));
                }

                this.MonthItems = items;
                return;
            }

            foreach (var item in existing)
            {
                item.IsSelectable = item.Value >= first && item.Value <= last;

                // A custom MonthFormat can include the year, so the text is refreshed alongside the range.
                item.DisplayText = DateSpinnerCalendar.FormatMonth(_pendingDate.Year, item.Value, culture, this.MonthDisplayMode, this.MonthFormat);
            }
        }

        private void RebuildDayItems()
        {
            var culture = this.EffectiveCulture;
            int daysInMonth = DateSpinnerCalendar.DaysInMonth(_pendingDate.Year, _pendingDate.Month);
            var (first, last) = DateSpinnerCalendar.GetDayRange(_pendingDate.Year, _pendingDate.Month, this.MinimumDate, this.MaximumDate);
            var existing = this.DayItems;

            // The day collection is only re-created when the month's length changes, which is what makes moving
            // between two 31 day months free.
            if (existing == null || existing.Count != daysInMonth)
            {
                var items = new List<DateSpinnerItem>(daysInMonth);

                for (int day = 1; day <= daysInMonth; day++)
                {
                    string text = DateSpinnerCalendar.FormatDay(_pendingDate.Year, _pendingDate.Month, day, culture, this.DayFormat);
                    items.Add(new DateSpinnerItem(day, text, day >= first && day <= last));
                }

                this.DayItems = items;
                return;
            }

            foreach (var item in existing)
            {
                item.IsSelectable = item.Value >= first && item.Value <= last;

                // A custom DayFormat can include the month or the day of week, so the text follows the month.
                item.DisplayText = DateSpinnerCalendar.FormatDay(_pendingDate.Year, _pendingDate.Month, item.Value, culture, this.DayFormat);
            }
        }

        /// <summary>
        /// Re-renders every wheel entry after a format, culture, or display mode change without disturbing the
        /// collections themselves, so the wheels keep their scroll position.
        /// </summary>
        private void RefreshItemText()
        {
            var culture = this.EffectiveCulture;

            if (this.MonthItems != null)
            {
                foreach (var item in this.MonthItems)
                {
                    item.DisplayText = DateSpinnerCalendar.FormatMonth(_pendingDate.Year, item.Value, culture, this.MonthDisplayMode, this.MonthFormat);
                }
            }

            if (this.DayItems != null)
            {
                foreach (var item in this.DayItems)
                {
                    item.DisplayText = DateSpinnerCalendar.FormatDay(_pendingDate.Year, _pendingDate.Month, item.Value, culture, this.DayFormat);
                }
            }

            if (this.YearItems != null)
            {
                foreach (var item in this.YearItems)
                {
                    item.DisplayText = DateSpinnerCalendar.FormatYear(item.Value, culture, this.YearFormat);
                }
            }
        }

        #endregion

        #region Presentation

        /// <summary>
        /// Recomputes the field order and the dividers between fields from the culture's short date pattern and
        /// the current field visibility.
        /// </summary>
        private void RefreshFieldOrder()
        {
            var order = DateSpinnerCalendar.GetFieldOrder(this.EffectiveCulture);

            this.MonthFieldIndex = IndexOfField(order, DateSpinnerField.Month);
            this.DayFieldIndex = IndexOfField(order, DateSpinnerField.Day);
            this.YearFieldIndex = IndexOfField(order, DateSpinnerField.Year);

            var visibleColumns = new[]
            {
                this.IsFieldVisible(order[0]),
                this.IsFieldVisible(order[1]),
                this.IsFieldVisible(order[2])
            };

            var visibleWidth = new GridLength(1, GridUnitType.Star);
            this.FirstFieldColumnWidth = visibleColumns[0] ? visibleWidth : new GridLength(0);
            this.SecondFieldColumnWidth = visibleColumns[1] ? visibleWidth : new GridLength(0);
            this.ThirdFieldColumnWidth = visibleColumns[2] ? visibleWidth : new GridLength(0);

            // A field draws a divider on its left edge unless it is the first visible one.
            bool seenVisible = false;

            foreach (var field in order)
            {
                bool visible = this.IsFieldVisible(field);

                var visibility = visible && seenVisible ? Visibility.Visible : Visibility.Collapsed;

                switch (field)
                {
                    case DateSpinnerField.Month:
                        this.MonthSeparatorVisibility = visibility;
                        break;
                    case DateSpinnerField.Day:
                        this.DaySeparatorVisibility = visibility;
                        break;
                    default:
                        this.YearSeparatorVisibility = visibility;
                        break;
                }

                seenVisible |= visible;
            }
        }

        private bool IsFieldVisible(DateSpinnerField field)
        {
            return field switch
            {
                DateSpinnerField.Month => this.IsMonthVisible,
                DateSpinnerField.Day => this.IsDayVisible,
                _ => this.IsYearVisible
            };
        }

        /// <summary>
        /// The position of a field in a culture's field order. <see cref="IReadOnlyList{T}"/> has no IndexOf.
        /// </summary>
        private static int IndexOfField(IReadOnlyList<DateSpinnerField> order, DateSpinnerField field)
        {
            for (int i = 0; i < order.Count; i++)
            {
                if (order[i] == field)
                {
                    return i;
                }
            }

            return 0;
        }

        /// <summary>
        /// Rebuilds the three strings shown on the closed entry surface.
        /// </summary>
        private void RefreshDisplayText()
        {
            var culture = this.EffectiveCulture;
            var date = this.SelectedDate;

            if (date.HasValue)
            {
                this.MonthText = DateSpinnerCalendar.FormatMonth(date.Value.Year, date.Value.Month, culture, this.MonthDisplayMode, this.MonthFormat);
                this.DayText = DateSpinnerCalendar.FormatDay(date.Value.Year, date.Value.Month, date.Value.Day, culture, this.DayFormat);
                this.YearText = DateSpinnerCalendar.FormatYear(date.Value.Year, culture, this.YearFormat);
                return;
            }

            this.MonthText = this.ResolveText(this.MonthPlaceholderText, MonthPlaceholderTextKey, "Month");
            this.DayText = this.ResolveText(this.DayPlaceholderText, DayPlaceholderTextKey, "Day");
            this.YearText = this.ResolveText(this.YearPlaceholderText, YearPlaceholderTextKey, "Year");
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
                    if (this.CommitMode == DateSpinnerCommitMode.Immediate)
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
            return new DateSpinnerAutomationPeer(this);
        }
    }
}
