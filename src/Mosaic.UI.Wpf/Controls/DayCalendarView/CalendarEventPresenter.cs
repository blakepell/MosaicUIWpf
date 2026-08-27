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

using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Presents one source item inside a <see cref="DayCalendarView"/> timeline.
    /// </summary>
    public class CalendarEventPresenter : ContentControl
    {
        private Point _pointerDownPoint;
        private bool _pointerPressed;
        private bool _isDragging;

        private static readonly DependencyPropertyKey IsSelectedPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsSelected), typeof(bool), typeof(CalendarEventPresenter), new PropertyMetadata(false));

        private static readonly DependencyPropertyKey IsReadOnlyPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(IsReadOnly), typeof(bool), typeof(CalendarEventPresenter), new PropertyMetadata(false));

        private static readonly DependencyPropertyKey CanDeletePropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(CanDelete), typeof(bool), typeof(CalendarEventPresenter), new PropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="IsSelected"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsSelectedProperty = IsSelectedPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="IsReadOnly"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsReadOnlyProperty = IsReadOnlyPropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="CanDelete"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CanDeleteProperty = CanDeletePropertyKey.DependencyProperty;

        /// <summary>
        /// Identifies the <see cref="DisplayTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayTitleProperty = DependencyProperty.Register(
            nameof(DisplayTitle), typeof(string), typeof(CalendarEventPresenter), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DisplayDescription"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayDescriptionProperty = DependencyProperty.Register(
            nameof(DisplayDescription), typeof(string), typeof(CalendarEventPresenter), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="DisplayTimeText"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DisplayTimeTextProperty = DependencyProperty.Register(
            nameof(DisplayTimeText), typeof(string), typeof(CalendarEventPresenter), new PropertyMetadata(string.Empty));

        /// <summary>
        /// Identifies the <see cref="CornerRadius"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerRadiusProperty = DependencyProperty.Register(
            nameof(CornerRadius), typeof(CornerRadius), typeof(CalendarEventPresenter),
            new PropertyMetadata(new CornerRadius(0)));

        /// <summary>
        /// Gets a value that indicates whether this event is the calendar's selected item.
        /// </summary>
        public bool IsSelected => (bool)GetValue(IsSelectedProperty);

        /// <summary>
        /// Gets a value that indicates whether this event cannot be moved by dragging.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if the event cannot be moved; otherwise, <see langword="false"/>.
        /// </value>
        public bool IsReadOnly => (bool)GetValue(IsReadOnlyProperty);

        /// <summary>
        /// Gets a value that indicates whether this event may be deleted from the calendar.
        /// </summary>
        /// <value>
        /// <see langword="true"/> if pressing <c>Delete</c> while the event has focus may remove it;
        /// otherwise, <see langword="false"/>.
        /// </value>
        public bool CanDelete => (bool)GetValue(CanDeleteProperty);

        /// <summary>
        /// Gets or sets the resolved title displayed by the default event template.
        /// </summary>
        public string DisplayTitle
        {
            get => (string)GetValue(DisplayTitleProperty);
            set => SetValue(DisplayTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the resolved description displayed by the default event template.
        /// </summary>
        public string DisplayDescription
        {
            get => (string)GetValue(DisplayDescriptionProperty);
            set => SetValue(DisplayDescriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the resolved time range displayed by the default event template.
        /// </summary>
        public string DisplayTimeText
        {
            get => (string)GetValue(DisplayTimeTextProperty);
            set => SetValue(DisplayTimeTextProperty, value);
        }

        /// <summary>
        /// Gets or sets the corner radius of the event card.
        /// </summary>
        public CornerRadius CornerRadius
        {
            get => (CornerRadius)GetValue(CornerRadiusProperty);
            set => SetValue(CornerRadiusProperty, value);
        }

        internal DayCalendarView? Owner { get; set; }

        internal object EventItem => Content!;

        internal DateTime ActualStart { get; set; }

        internal DateTime ActualEnd { get; set; }

        internal DateTime ClippedStart { get; set; }

        internal DateTime ClippedEnd { get; set; }

        internal DateTime DisplayStart => _isDragging && Owner != null
            ? Max(Owner.SelectedDate.Date, Owner.DragPreviewStart)
            : ClippedStart;

        internal DateTime DisplayEnd => _isDragging && Owner != null
            ? Min(Owner.SelectedDate.Date.AddDays(1), Owner.DragPreviewEnd)
            : ClippedEnd;

        static CalendarEventPresenter()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(CalendarEventPresenter),
                new FrameworkPropertyMetadata(typeof(CalendarEventPresenter)));
            FocusableProperty.OverrideMetadata(typeof(CalendarEventPresenter), new FrameworkPropertyMetadata(true));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CalendarEventPresenter"/> class.
        /// </summary>
        public CalendarEventPresenter()
        {
            Cursor = Cursors.Hand;
        }

        internal void SetSelected(bool value)
        {
            SetValue(IsSelectedPropertyKey, value);
            AutomationProperties.SetItemStatus(this, value ? "Selected" : string.Empty);
        }

        internal void SetReadOnly(bool value)
        {
            SetValue(IsReadOnlyPropertyKey, value);
        }

        internal void SetCanDelete(bool value)
        {
            SetValue(CanDeletePropertyKey, value);
        }

        internal void SetDragging(bool value)
        {
            _isDragging = value;
            Panel.SetZIndex(this, value ? 1000 : 0);
            Opacity = value ? 0.82 : 1.0;
        }

        internal void InvokeFromAutomation()
        {
            Owner?.ActivateEvent(this);
        }

        /// <inheritdoc />
        protected override AutomationPeer OnCreateAutomationPeer()
        {
            return new CalendarEventPresenterAutomationPeer(this);
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonDown(e);
            if (!IsEnabled || Owner?.TimelinePanel == null)
            {
                return;
            }

            Focus();
            _pointerPressed = true;
            _pointerDownPoint = e.GetPosition(Owner.TimelinePanel);
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_pointerPressed || Owner?.TimelinePanel == null || e.LeftButton != MouseButtonState.Pressed)
            {
                return;
            }

            var position = e.GetPosition(Owner.TimelinePanel);
            if (!_isDragging)
            {
                var delta = position - _pointerDownPoint;
                if (Math.Abs(delta.Y) < SystemParameters.MinimumVerticalDragDistance)
                {
                    return;
                }

                if (!Owner.BeginEventDrag(this, _pointerDownPoint.Y))
                {
                    return;
                }

                _isDragging = true;
                CaptureMouse();
            }

            Owner.UpdateEventDrag(position.Y);
            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseLeftButtonUp(e);
            if (!_pointerPressed)
            {
                return;
            }

            _pointerPressed = false;
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                Owner?.CompleteEventDrag();
            }
            else
            {
                Owner?.ActivateEvent(this);
            }

            e.Handled = true;
        }

        /// <inheritdoc />
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Key == Key.Escape && _isDragging)
            {
                _pointerPressed = false;
                _isDragging = false;
                ReleaseMouseCapture();
                Owner?.CancelEventDrag();
                e.Handled = true;
            }
            else if (e.Key is Key.Enter or Key.Space)
            {
                Owner?.ActivateEvent(this);
                e.Handled = true;
            }
            else if (e.Key == Key.Delete && !_isDragging && CanDelete && Owner != null)
            {
                Owner.RequestDeleteEvent(this);
                e.Handled = true;
            }
        }

        /// <inheritdoc />
        protected override void OnLostMouseCapture(MouseEventArgs e)
        {
            base.OnLostMouseCapture(e);
            if (_isDragging)
            {
                _pointerPressed = false;
                _isDragging = false;
                Owner?.CancelEventDrag();
            }
        }

        private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

        private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;
    }

    /// <summary>
    /// Exposes calendar-event activation to UI Automation clients.
    /// </summary>
    internal sealed class CalendarEventPresenterAutomationPeer : FrameworkElementAutomationPeer, IInvokeProvider
    {
        internal CalendarEventPresenterAutomationPeer(CalendarEventPresenter owner)
            : base(owner)
        {
        }

        protected override string GetClassNameCore() => nameof(CalendarEventPresenter);

        protected override AutomationControlType GetAutomationControlTypeCore() => AutomationControlType.ListItem;

        protected override string GetNameCore()
        {
            var name = base.GetNameCore();
            return !string.IsNullOrWhiteSpace(name) ? name : ((CalendarEventPresenter)Owner).DisplayTitle;
        }

        public override object? GetPattern(PatternInterface patternInterface)
        {
            return patternInterface == PatternInterface.Invoke ? this : base.GetPattern(patternInterface);
        }

        void IInvokeProvider.Invoke()
        {
            if (!IsEnabled())
            {
                throw new ElementNotEnabledException();
            }

            ((CalendarEventPresenter)Owner).Dispatcher.BeginInvoke(((CalendarEventPresenter)Owner).InvokeFromAutomation);
        }
    }
}
