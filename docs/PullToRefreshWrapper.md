# PullToRefreshWrapper

Wrap a ListView, DataGrid, or ScrollViewer to request refresh by dragging downward at the top of its scroll range. Normal clicks pass through until the vertical drag threshold is crossed. Short pulls spring back without refreshing. F5 and Ctrl+R work while focus is inside the wrapper; UI Automation exposes the Invoke pattern.

```xml
<mosaic:PullToRefreshWrapper
    IsRefreshing="{Binding IsRefreshing, Mode=TwoWay}"
    RefreshCommand="{Binding RefreshCommand}"
    PullThreshold="100" PullFriction="0.5">
    <ListView ItemsSource="{Binding Items}" />
</mosaic:PullToRefreshWrapper>
```

Use `xmlns:mosaic="http://schemas.apexgate.net/wpf/mosaic-ui"` and merge a Mosaic ThemeManager into your resources.

The wrapper sets `IsRefreshing` to true before executing `RefreshCommand`. The consumer must set it to false when its asynchronous work finishes, preferably in a `finally` block. An async command should prevent concurrent executions. If the command is absent or cannot execute, the pull snaps back. `RefreshCommandParameter` is forwarded to both `CanExecute` and `Execute`. Setting `IsRefreshing` directly controls the indicator without invoking the command.

`PullThreshold` is the translated distance in WPF device-independent pixels, after friction. With the defaults (100 and 0.5), drag 200 pixels to trigger refresh. Threshold must be finite and positive; friction must be finite and in `(0, 1]`. `PullProgress` (0–100) and `IsPullReady` are read-only properties available to custom templates.

The default indicator uses a RadialProgressBar in Pie mode with a theme-aware radial gradient. Its value follows pull progress, then repeats from 0 to 100 every 1.1 seconds during refresh. Content settles at the indicator height (60 pixels by default), then returns with a cubic ease-out animation when refresh completes.

Template parts: `PART_Content` (ContentPresenter), `PART_Indicator` (FrameworkElement), `PART_ContentTransform` and `PART_IndicatorTransform` (TranslateTransform), and `PART_Progress` (RadialProgressBar). Keep the indicator above the content, with a negative top margin matching its height, and clip the containing grid. Missing optional visuals are handled safely.

Give the wrapper a constrained height so its child can scroll. It uses the first ScrollViewer in the wrapped visual tree; avoid wrapping several independent scroll regions in one wrapper. Scrollbar drags are excluded. The demo includes an asynchronous random feed and item buttons for checking click coexistence.
