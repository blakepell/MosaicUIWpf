/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Media.Animation;

namespace Mosaic.UI.Wpf.Controls;

/// <summary>
/// Decorates scrollable content with a mouse and touch pull-to-refresh gesture.
/// </summary>
/// <remarks>
/// A pull starts only at the top of the inner ScrollViewer. Set IsRefreshing to false when
/// the refresh task completes. F5 and Ctrl+R request a refresh while focus is inside the wrapper.
/// Setting IsRefreshing directly updates the visual without executing RefreshCommand.
/// </remarks>
[TemplatePart(Name = "PART_Content", Type = typeof(ContentPresenter))]
[TemplatePart(Name = "PART_Indicator", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_ContentTransform", Type = typeof(TranslateTransform))]
[TemplatePart(Name = "PART_IndicatorTransform", Type = typeof(TranslateTransform))]
[TemplatePart(Name = "PART_Progress", Type = typeof(RadialProgressBar))]
public class PullToRefreshWrapper : ContentControl
{
    private ContentPresenter? _presenter;
    private FrameworkElement? _indicator;
    private TranslateTransform? _contentTransform;
    private TranslateTransform? _indicatorTransform;
    private RadialProgressBar? _progress;
    private ScrollViewer? _scrollViewer;
    private Point? _mouseStart;
    private bool _pulling;
    private double _touchDistance;
    private bool _touchEligible;
    private double _distance;

    /// <summary>
    /// Identifies the IsRefreshing dependency property.
    /// </summary>
    public static readonly DependencyProperty IsRefreshingProperty = DependencyProperty.Register(
        nameof(IsRefreshing), typeof(bool), typeof(PullToRefreshWrapper),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
            OnIsRefreshingChanged));

    /// <summary>
    /// Identifies the RefreshCommand dependency property.
    /// </summary>
    public static readonly DependencyProperty RefreshCommandProperty = DependencyProperty.Register(
        nameof(RefreshCommand), typeof(ICommand), typeof(PullToRefreshWrapper));

    /// <summary>
    /// Identifies the RefreshCommandParameter dependency property.
    /// </summary>
    public static readonly DependencyProperty RefreshCommandParameterProperty = DependencyProperty.Register(
        nameof(RefreshCommandParameter), typeof(object), typeof(PullToRefreshWrapper));

    /// <summary>
    /// Identifies the PullThreshold dependency property.
    /// </summary>
    public static readonly DependencyProperty PullThresholdProperty = DependencyProperty.Register(
        nameof(PullThreshold), typeof(double), typeof(PullToRefreshWrapper),
        new PropertyMetadata(100d, OnPullSettingsChanged), IsPositiveFinite);

    /// <summary>
    /// Identifies the PullFriction dependency property.
    /// </summary>
    public static readonly DependencyProperty PullFrictionProperty = DependencyProperty.Register(
        nameof(PullFriction), typeof(double), typeof(PullToRefreshWrapper),
        new PropertyMetadata(0.5d), value => IsPositiveFinite(value) && (double)value <= 1);

    private static readonly DependencyPropertyKey PullProgressPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(PullProgress), typeof(double), typeof(PullToRefreshWrapper), new PropertyMetadata(0d));

    /// <summary>
    /// Identifies the read-only PullProgress dependency property.
    /// </summary>
    public static readonly DependencyProperty PullProgressProperty = PullProgressPropertyKey.DependencyProperty;

    private static readonly DependencyPropertyKey IsPullReadyPropertyKey = DependencyProperty.RegisterReadOnly(
        nameof(IsPullReady), typeof(bool), typeof(PullToRefreshWrapper), new PropertyMetadata(false));

    /// <summary>
    /// Identifies the read-only IsPullReady dependency property.
    /// </summary>
    public static readonly DependencyProperty IsPullReadyProperty = IsPullReadyPropertyKey.DependencyProperty;

    /// <summary>
    /// Gets or sets whether refresh is running. Set to false when the consumer's task finishes.
    /// </summary>
    [Category("Behavior")]
    public bool IsRefreshing { get => (bool)GetValue(IsRefreshingProperty); set => SetValue(IsRefreshingProperty, value); }

    /// <summary>
    /// Gets or sets the command executed after a qualifying pull or keyboard refresh request.
    /// </summary>
    [Category("Action")]
    public ICommand? RefreshCommand { get => (ICommand?)GetValue(RefreshCommandProperty); set => SetValue(RefreshCommandProperty, value); }

    /// <summary>
    /// Gets or sets the parameter passed to the refresh command.
    /// </summary>
    [Category("Action")]
    public object? RefreshCommandParameter { get => GetValue(RefreshCommandParameterProperty); set => SetValue(RefreshCommandParameterProperty, value); }

    /// <summary>
    /// Gets or sets the required translated pull distance in device-independent pixels. Defaults to 100.
    /// </summary>
    [Category("Behavior")]
    public double PullThreshold { get => (double)GetValue(PullThresholdProperty); set => SetValue(PullThresholdProperty, value); }

    /// <summary>
    /// Gets or sets the drag multiplier, greater than zero and at most one. Defaults to 0.5.
    /// </summary>
    [Category("Behavior")]
    public double PullFriction { get => (double)GetValue(PullFrictionProperty); set => SetValue(PullFrictionProperty, value); }

    /// <summary>
    /// Gets the pull percentage, clamped to the range 0–100.
    /// </summary>
    public double PullProgress => (double)GetValue(PullProgressProperty);

    /// <summary>
    /// Gets whether the current pull has reached the refresh threshold.
    /// </summary>
    public bool IsPullReady => (bool)GetValue(IsPullReadyProperty);

    static PullToRefreshWrapper()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(PullToRefreshWrapper), new FrameworkPropertyMetadata(typeof(PullToRefreshWrapper)));
        IsManipulationEnabledProperty.OverrideMetadata(typeof(PullToRefreshWrapper), new FrameworkPropertyMetadata(true));
    }

    /// <summary>
    /// Initializes a new instance of the PullToRefreshWrapper class.
    /// </summary>
    public PullToRefreshWrapper()
    {
        // ScrollViewer handles its manipulation events before they bubble to the decorator.
        // Observe handled events too, and suppress boundary bounce only during our pull.
        AddHandler(ManipulationStartingEvent, new EventHandler<ManipulationStartingEventArgs>(OnTouchManipulationStarting), true);
        AddHandler(ManipulationDeltaEvent, new EventHandler<ManipulationDeltaEventArgs>(OnTouchManipulationDelta), true);
        AddHandler(ManipulationInertiaStartingEvent, new EventHandler<ManipulationInertiaStartingEventArgs>(OnTouchManipulationInertiaStarting), true);
        AddHandler(ManipulationCompletedEvent, new EventHandler<ManipulationCompletedEventArgs>(OnTouchManipulationCompleted), true);
        AddHandler(ManipulationBoundaryFeedbackEvent, new EventHandler<ManipulationBoundaryFeedbackEventArgs>(
            (_, e) => { if (_pulling && _touchEligible) e.Handled = true; }), true);
        Loaded += (_, _) => { _scrollViewer = null; UpdateRefreshingVisual(false); };
        Unloaded += (_, _) =>
        {
            ResetGesture();
            StopAnimations();
            _scrollViewer = null;
        };
        IsEnabledChanged += (_, _) => { if (!IsEnabled) CancelPull(); };
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        ResetGesture();
        StopAnimations();
        base.OnApplyTemplate();
        _presenter = GetTemplateChild("PART_Content") as ContentPresenter;
        _indicator = GetTemplateChild("PART_Indicator") as FrameworkElement;
        _contentTransform = GetTemplateChild("PART_ContentTransform") as TranslateTransform;
        _indicatorTransform = GetTemplateChild("PART_IndicatorTransform") as TranslateTransform;
        _progress = GetTemplateChild("PART_Progress") as RadialProgressBar;
        _scrollViewer = null;
        UpdateRefreshingVisual(false);
    }

    /// <inheritdoc />
    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        _scrollViewer = null;
        CancelPull();
    }

    /// <summary>
    /// Requests refresh when enabled, idle, and the command can execute.
    /// </summary>
    /// <returns>True if the command was executed; otherwise, false.</returns>
    public bool RequestRefresh()
    {
        var command = RefreshCommand;
        var parameter = RefreshCommandParameter;
        if (!IsEnabled || IsRefreshing || command == null || !command.CanExecute(parameter)) return false;
        ResetGesture();
        SetCurrentValue(IsRefreshingProperty, true);
        try
        {
            command.Execute(parameter);
        }
        catch
        {
            SetCurrentValue(IsRefreshingProperty, false);
            throw;
        }
        return true;
    }

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);
        if ((e.Key == Key.F5 || (e.Key == Key.R && Keyboard.Modifiers == ModifierKeys.Control)) && RequestRefresh())
            e.Handled = true;
        else if (e.Key == Key.Escape && _pulling)
        {
            CancelPull();
            e.Handled = true;
        }
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonDown(e);
        // Touch has its own manipulation path; ignore promoted mouse events and scrollbar drags.
        if (e.StylusDevice == null && !IsRefreshing && IsEnabled && IsAtTop() && !IsScrollBar(e.OriginalSource as DependencyObject))
            _mouseStart = e.GetPosition(this);
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        if (_mouseStart is not Point start || IsRefreshing) return;
        if (e.LeftButton != MouseButtonState.Pressed) { CancelPull(); return; }
        var delta = e.GetPosition(this) - start;
        if (!_pulling)
        {
            if (!IsAtTop() || delta.Y < -SystemParameters.MinimumVerticalDragDistance ||
                Math.Abs(delta.X) > SystemParameters.MinimumHorizontalDragDistance && Math.Abs(delta.X) > delta.Y)
            { _mouseStart = null; return; }
            if (delta.Y <= SystemParameters.MinimumVerticalDragDistance || !CaptureMouse()) return;
            _pulling = true;
        }
        SetPullDistance(Math.Max(0, delta.Y * PullFriction));
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override void OnPreviewMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseLeftButtonUp(e);
        if (!_pulling) { _mouseStart = null; return; }
        e.Handled = true;
        FinishPull();
    }

    /// <inheritdoc />
    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        base.OnLostMouseCapture(e);
        if (_pulling && _mouseStart != null && !IsMouseCaptured) CancelPull();
    }

    private void OnTouchManipulationStarting(object? sender, ManipulationStartingEventArgs e)
    {
        _touchEligible = !IsRefreshing && IsEnabled && IsAtTop() && !IsScrollBar(e.OriginalSource as DependencyObject);
        if (!_touchEligible) return;
        _touchDistance = 0;
        e.ManipulationContainer = this;
        e.Mode = ManipulationModes.TranslateY;
        e.Handled = true;
    }

    private void OnTouchManipulationDelta(object? sender, ManipulationDeltaEventArgs e)
    {
        if (!_touchEligible || IsRefreshing || !IsEnabled) return;
        if (!_pulling && (!IsAtTop() || e.CumulativeManipulation.Translation.Y < -SystemParameters.MinimumVerticalDragDistance))
        { _touchEligible = false; return; }
        _touchDistance = Math.Max(0, _touchDistance + e.DeltaManipulation.Translation.Y);
        if (!_pulling && _touchDistance <= SystemParameters.MinimumVerticalDragDistance) return;
        _pulling = true;
        // A reversal belongs to the active pull rather than scrolling the list underneath it.
        _scrollViewer?.ScrollToVerticalOffset(0);
        SetPullDistance(_touchDistance * PullFriction);
        e.Handled = true;
        if (e.IsInertial) e.Complete();
    }

    private void OnTouchManipulationInertiaStarting(object? sender, ManipulationInertiaStartingEventArgs e)
    {
        if (!_touchEligible || !_pulling) return;
        // Release determines the threshold; momentum must not turn a short pull into a refresh.
        e.TranslationBehavior.InitialVelocity = new Vector(0, 0);
        FinishPull();
        e.Handled = true;
    }

    private void OnTouchManipulationCompleted(object? sender, ManipulationCompletedEventArgs e)
    {
        if (!_touchEligible || !_pulling) { _touchEligible = false; return; }
        FinishPull();
        e.Handled = true;
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new PullToRefreshWrapperAutomationPeer(this);

    private static bool IsPositiveFinite(object value) => double.IsFinite((double)value) && (double)value > 0;

    private static void OnPullSettingsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wrapper = (PullToRefreshWrapper)d;
        wrapper.UpdatePullProgress();
    }

    private static void OnIsRefreshingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var wrapper = (PullToRefreshWrapper)d;
        wrapper.ResetGesture();
        wrapper.UpdateRefreshingVisual(wrapper.IsLoaded);
        if (UIElementAutomationPeer.FromElement(wrapper) is PullToRefreshWrapperAutomationPeer peer)
            peer.RaiseRefreshStateChanged((bool)e.OldValue, (bool)e.NewValue);
    }

    private bool IsAtTop()
    {
        if (_presenter == null) return false;
        // Revalidate ancestry in case a child control replaced its template or scroll viewer.
        if (_scrollViewer == null || !_presenter.IsAncestorOf(_scrollViewer))
            _scrollViewer = FindScrollViewer(_presenter);
        return _scrollViewer != null && _scrollViewer.VerticalOffset <= 0;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        if (root is ScrollViewer viewer) return viewer;
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
        {
            var found = FindScrollViewer(VisualTreeHelper.GetChild(root, i));
            if (found != null) return found;
        }
        return null;
    }

    private bool IsScrollBar(DependencyObject? source)
    {
        while (source != null && source != this)
        {
            if (source is ScrollBar) return true;
            source = source is Visual or System.Windows.Media.Media3D.Visual3D
                ? VisualTreeHelper.GetParent(source) : LogicalTreeHelper.GetParent(source);
        }
        return false;
    }

    private void SetPullDistance(double distance)
    {
        _distance = distance;
        SetTranslation(_contentTransform, distance, false);
        SetTranslation(_indicatorTransform, distance, false);
        UpdatePullProgress();
    }

    private void UpdatePullProgress()
    {
        SetValue(PullProgressPropertyKey, Math.Clamp(_distance / PullThreshold * 100, 0, 100));
        SetValue(IsPullReadyPropertyKey, _distance >= PullThreshold);
        if (_progress != null && !IsRefreshing) _progress.SetCurrentValue(RangeBase.ValueProperty, PullProgress);
    }

    private void FinishPull()
    {
        var ready = _distance >= PullThreshold;
        ResetGesture();
        if (!ready || !RequestRefresh()) UpdateRefreshingVisual(true);
    }

    private void CancelPull()
    {
        ResetGesture();
        UpdateRefreshingVisual(IsLoaded);
    }

    private void ResetGesture()
    {
        _pulling = false;
        _mouseStart = null;
        _touchDistance = 0;
        _touchEligible = false;
        if (IsMouseCaptured) ReleaseMouseCapture();
    }

    private void UpdateRefreshingVisual(bool animate)
    {
        _distance = 0;
        UpdatePullProgress();
        var height = _indicator?.ActualHeight > 0 ? _indicator.ActualHeight : 60;
        SetTranslation(_contentTransform, IsRefreshing ? height : 0, animate);
        SetTranslation(_indicatorTransform, IsRefreshing ? height : 0, animate);
        if (_progress == null) return;
        _progress.BeginAnimation(RangeBase.ValueProperty, null);
        _progress.SetCurrentValue(RangeBase.ValueProperty, 0d);
        if (IsRefreshing && IsLoaded)
            _progress.BeginAnimation(RangeBase.ValueProperty, new DoubleAnimation(0, 100, TimeSpan.FromSeconds(1.1))
            { RepeatBehavior = RepeatBehavior.Forever });
    }

    private static void SetTranslation(TranslateTransform? transform, double target, bool animate)
    {
        if (transform == null) return;
        var from = transform.Y;
        transform.BeginAnimation(TranslateTransform.YProperty, null);
        transform.Y = target;
        if (animate)
            transform.BeginAnimation(TranslateTransform.YProperty,
                new DoubleAnimation(from, target, TimeSpan.FromMilliseconds(240))
                { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }, FillBehavior = FillBehavior.Stop });
    }

    private void StopAnimations()
    {
        _progress?.BeginAnimation(RangeBase.ValueProperty, null);
        _contentTransform?.BeginAnimation(TranslateTransform.YProperty, null);
        _indicatorTransform?.BeginAnimation(TranslateTransform.YProperty, null);
    }
}
