/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.Memory;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using Xunit;

namespace Mosaic.UI.Wpf.Tests
{
    /// <summary>
    /// Tests for the <see cref="Ribbon"/>: the adaptive layout invariants, selection fallback, the Quick Access
    /// Toolbar, persistence, key tip generation and the shared command policy. WPF tests run on STA threads.
    /// </summary>
    public class RibbonTests
    {
        private const string RibbonXaml = """
            <mosaic:Ribbon xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                           xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                           xmlns:mosaic="clr-namespace:Mosaic.UI.Wpf.Controls;assembly=Mosaic.UI.Wpf">
              <mosaic:Ribbon.ContextualTabGroups>
                <mosaic:RibbonContextualTabGroup Key="Picture" Header="Picture Tools" />
              </mosaic:Ribbon.ContextualTabGroups>
              <mosaic:Ribbon.Backstage>
                <mosaic:RibbonBackstage IsAnimationEnabled="False">
                  <mosaic:RibbonBackstageTabItem Header="Home"><TextBlock Text="Home page" /></mosaic:RibbonBackstageTabItem>
                  <mosaic:RibbonBackstageButton Content="Save" />
                </mosaic:RibbonBackstage>
              </mosaic:Ribbon.Backstage>
              <mosaic:RibbonTabItem Header="Home" Key="Home">
                <mosaic:RibbonGroup Header="Clipboard">
                  <mosaic:RibbonSplitButton Key="Paste" Label="Paste" Glyph="&#xE77F;" PreferredVariant="Large">
                    <mosaic:RibbonMenuItem Header="Keep Text Only" Key="PasteText" />
                  </mosaic:RibbonSplitButton>
                  <mosaic:RibbonButton Key="Cut" Label="Cut" Glyph="&#xE8C6;" />
                  <mosaic:RibbonButton Key="Copy" Label="Copy" Glyph="&#xE8C8;" />
                  <mosaic:RibbonToggleButton Key="Painter" Label="Format Painter" Glyph="&#xE771;" />
                </mosaic:RibbonGroup>
                <mosaic:RibbonGroup Header="Font">
                  <mosaic:RibbonControlGroup>
                    <mosaic:RibbonComboBox Key="FontFamily" Label="Font" ShowLabel="False" InputWidth="120" />
                    <mosaic:RibbonComboBox Key="FontSize" Label="Size" ShowLabel="False" InputWidth="48" />
                  </mosaic:RibbonControlGroup>
                  <mosaic:RibbonControlGroup>
                    <mosaic:RibbonToggleButton Key="Bold" Label="Bold" Glyph="&#xE8DD;" />
                    <mosaic:RibbonToggleButton Key="Italic" Label="Italic" Glyph="&#xE8DB;" />
                    <mosaic:RibbonToggleButton Key="Underline" Label="Underline" Glyph="&#xE8DC;" />
                  </mosaic:RibbonControlGroup>
                </mosaic:RibbonGroup>
                <mosaic:RibbonGroup Header="Styles" ChildOverflowTarget="Group">
                  <mosaic:RibbonGallery Key="Styles" Label="Styles" InlineColumns="4" />
                </mosaic:RibbonGroup>
                <mosaic:RibbonGroup Header="Editing">
                  <mosaic:RibbonButton Key="Find" Label="Find" Glyph="&#xE721;" PreferredVariant="Large" mosaic:RibbonItem.CollapseBehavior="Never" />
                  <mosaic:RibbonButton Key="Replace" Label="Replace" Glyph="&#xE8AC;" />
                  <mosaic:RibbonButton Key="SelectAll" Label="Select All" Glyph="&#xE8B3;" mosaic:RibbonItem.CollapseBehavior="Always" />
                </mosaic:RibbonGroup>
                <mosaic:RibbonGroup Header="Voice">
                  <mosaic:RibbonButton Key="Dictate" Label="Dictate" Glyph="&#xE720;" PreferredVariant="Large" />
                  <mosaic:RibbonButton Key="ReadAloud" Label="Read Aloud" Glyph="&#xE767;" PreferredVariant="Large" />
                </mosaic:RibbonGroup>
              </mosaic:RibbonTabItem>
              <mosaic:RibbonTabItem Header="Insert" Key="Insert">
                <mosaic:RibbonGroup Header="Illustrations">
                  <mosaic:RibbonButton Key="Pictures" Label="Pictures" Glyph="&#xE8B9;" PreferredVariant="Large" />
                </mosaic:RibbonGroup>
              </mosaic:RibbonTabItem>
              <mosaic:RibbonTabItem Header="Picture Format" Key="PictureFormat" ContextualTabGroupKey="Picture">
                <mosaic:RibbonGroup Header="Adjust">
                  <mosaic:RibbonButton Key="RemoveBackground" Label="Remove Background" Glyph="&#xE7C8;" PreferredVariant="Large" />
                </mosaic:RibbonGroup>
              </mosaic:RibbonTabItem>
            </mosaic:Ribbon>
            """;

        private static void RunSta(Action action)
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    failure = ex;
                }
                finally
                {
                    Dispatcher.CurrentDispatcher.InvokeShutdown();
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (failure != null)
            {
                ExceptionDispatchInfo.Capture(failure).Throw();
            }
        }

        private static ThemeManager CreateTheme(MosaicThemeMode theme)
        {
            var manager = new ThemeManager();
            manager.BeginInit();
            manager.Theme = theme;
            manager.EndInit();
            return manager;
        }

        private static (Window Window, Ribbon Ribbon) Host(double width, MosaicThemeMode theme = MosaicThemeMode.Light)
        {
            var ribbon = (Ribbon)XamlReader.Parse(RibbonXaml);
            var panel = new RibbonContainerPanel();
            panel.Children.Add(ribbon);
            panel.Children.Add(new TextBox { Text = "Document" });

            var window = new Window
            {
                Width = width,
                Height = 600,
                ShowInTaskbar = false,
                ShowActivated = false,
                WindowStyle = WindowStyle.None,
                Left = -10000,
                Top = -10000,
                Content = panel
            };
            window.Resources.MergedDictionaries.Add(CreateTheme(theme));
            window.Show();
            Pump();
            return (window, ribbon);
        }

        private static void Pump()
        {
            var frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(() => frame.Continue = false));
            Dispatcher.PushFrame(frame);
        }

        private static void Resize(Window window, double width)
        {
            window.Width = width;
            window.UpdateLayout();
            Pump();
        }

        private static RibbonTabItem Tab(Ribbon ribbon, string key) => ribbon.Items.OfType<RibbonTabItem>().Single(t => t.Key == key);

        private static List<RibbonGroup> Groups(RibbonTabItem tab) => tab.Items.OfType<RibbonGroup>().ToList();

        private static string Signature(RibbonTabItem tab)
        {
            return string.Join("|", Groups(tab).Select(g =>
                g.Variant + ":" + string.Join(",", g.Items.OfType<UIElement>().Select(i => $"{RibbonItem.GetVariant(i)}{(RibbonItem.GetIsOverflowed(i) ? "*" : string.Empty)}"))));
        }

        [Fact]
        public void Small_Image_Opts_Into_Adaptive_Rendering_In_Each_Presentation()
        {
            RunSta(() =>
            {
                var theme = CreateTheme(MosaicThemeMode.Light);
                try
                {
                    AppServices.AddSingleton(theme);
                }
                catch (InvalidOperationException)
                {
                    // Another test registered the process-wide theme manager first.
                }

                var source = new RibbonButton { SmallImage = new DrawingImage() };
                var mirror = new RibbonMenuItem();
                RibbonPresentationFactory.BindVisuals(mirror, source);
                source.Resources.MergedDictionaries.Add(theme);
                mirror.Resources.MergedDictionaries.Add(theme);
                source.ApplyTemplate();
                mirror.ApplyTemplate();

                var smallIcon = (RibbonIconPresenter)source.Template.FindName("SmallIcon", source);
                var largeIcon = (RibbonIconPresenter)source.Template.FindName("LargeIcon", source);
                var menuIcon = (RibbonIconPresenter)mirror.Template.FindName("Icon", mirror);
                Assert.False(source.UseAdaptiveSmallImage);
                Assert.IsType<Image>(VisualTreeHelper.GetChild(smallIcon, 0));

                source.UseAdaptiveSmallImage = true;
                Assert.IsType<AdaptiveImage>(VisualTreeHelper.GetChild(smallIcon, 0));
                Assert.IsType<AdaptiveImage>(VisualTreeHelper.GetChild(largeIcon, 0));
                Assert.IsType<AdaptiveImage>(VisualTreeHelper.GetChild(menuIcon, 0));

                source.LargeImage = new DrawingImage();
                Assert.IsType<Image>(VisualTreeHelper.GetChild(largeIcon, 0));
                source.SmallImage = null;
                Assert.IsType<Image>(VisualTreeHelper.GetChild(smallIcon, 0));
            });
        }

        [Theory]
        [InlineData(MosaicThemeMode.Light)]
        [InlineData(MosaicThemeMode.Dark)]
        [InlineData(MosaicThemeMode.Black)]
        [InlineData(MosaicThemeMode.Blue)]
        [InlineData(MosaicThemeMode.HighContrast)]
        public void Templates_Load_And_Render_In_Every_Theme(MosaicThemeMode theme)
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1000, theme);
                try
                {
                    Assert.NotNull(ribbon.Template);
                    Assert.Equal("Home", ribbon.SelectedTab?.Header);
                    Assert.True(ribbon.ActualHeight > 100, $"Ribbon height was {ribbon.ActualHeight}");

                    ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                    window.UpdateLayout();
                    Pump();
                    Assert.True(ribbon.ActualHeight < 130, $"Simplified ribbon height was {ribbon.ActualHeight}");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Classic_Layout_Reduces_Monotonically_And_Is_Path_Independent()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1600);
                try
                {
                    var home = Tab(ribbon, "Home");
                    var down = new Dictionary<int, string>();
                    var previousScore = -1;

                    for (var width = 1600; width >= 260; width -= 20)
                    {
                        Resize(window, width);
                        down[width] = Signature(home);

                        // The more space is removed, the more reduced (never less) the configuration.
                        var score = Groups(home).Sum(g => (int)g.Variant);
                        Assert.True(score >= previousScore, $"Layout expanded while shrinking at {width}px");
                        previousScore = score;

                        // Either it fits or the strip scrolls; commands are never silently clipped.
                        var panel = home.GroupsPanel!;
                        var extent = Groups(home).Sum(g => g.DesiredSize.Width);
                        Assert.True(extent <= panel.ActualWidth + 0.5 || home.CanScrollRight || home.CanScrollLeft, $"Clipped without scrolling at {width}px");
                    }

                    Assert.Contains(Groups(home), g => g.Variant == VariantSize.Collapsed);

                    // Growing back visits the same states: a pure function of width, no hysteresis or oscillation.
                    for (var width = 260; width <= 1600; width += 20)
                    {
                        Resize(window, width);
                        Assert.Equal(down[width], Signature(home));
                    }

                    Assert.All(Groups(home), g => Assert.Equal(VariantSize.Large, g.Variant));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Simplified_Overflow_Keeps_Every_Command_Reachable_And_Honors_Collapse_Rules()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1400);
                try
                {
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                    var home = Tab(ribbon, "Home");

                    foreach (var width in new[] { 1400, 900, 600, 420 })
                    {
                        Resize(window, width);

                        var tabOverflowKeys = home.OverflowItems.Select(RibbonItem.GetSourceKey).ToHashSet();
                        foreach (var group in Groups(home))
                        {
                            var groupOverflowKeys = group.OverflowItems.Select(RibbonItem.GetSourceKey).ToHashSet();
                            foreach (var item in group.Items.OfType<FrameworkElement>())
                            {
                                var key = RibbonItem.GetKey(item);
                                if (RibbonItem.GetIsOverflowed(item))
                                {
                                    Assert.False(item.IsVisible, $"{key} is overflowed but still visible");
                                    var destination = group.ChildOverflowTarget == RibbonOverflowTarget.Group ? groupOverflowKeys : tabOverflowKeys;
                                    Assert.Contains(key, destination);
                                }
                                else
                                {
                                    Assert.True(item.IsVisible, $"{key} is neither inline nor in overflow at {width}px");
                                }

                                if (RibbonItem.GetCollapseBehavior(item) == ItemCollapseBehavior.Never)
                                {
                                    Assert.False(RibbonItem.GetIsOverflowed(item), $"{key} must never overflow");
                                }

                                if (RibbonItem.GetCollapseBehavior(item) == ItemCollapseBehavior.Always)
                                {
                                    Assert.True(RibbonItem.GetIsOverflowed(item), $"{key} must always overflow");
                                }
                            }
                        }
                    }

                    Assert.True(home.HasOverflowItems);

                    // Back to Classic: nothing stays hidden or overflowed.
                    ribbon.LayoutMode = RibbonLayoutMode.Classic;
                    Resize(window, 1600);
                    Assert.False(home.HasOverflowItems);
                    Assert.All(Groups(home).SelectMany(g => g.Items.OfType<UIElement>()), i => Assert.False(RibbonItem.GetIsOverflowed(i)));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Removing_The_Selected_Context_Falls_Back_To_An_Ordinary_Tab()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var context = ribbon.ContextualTabGroups.Single();
                    var pictureTab = Tab(ribbon, "PictureFormat");
                    Assert.Equal(Visibility.Collapsed, pictureTab.TabHeader!.Visibility);

                    context.IsActive = true;
                    Pump();
                    Assert.Equal(Visibility.Visible, pictureTab.TabHeader.Visibility);

                    ribbon.SelectTab(pictureTab, openWhenMinimized: false);
                    Assert.Same(pictureTab, ribbon.SelectedTab);

                    context.IsActive = false;
                    Pump();
                    Assert.Equal("Home", ribbon.SelectedTab?.Header);
                    Assert.Equal(Visibility.Collapsed, pictureTab.TabHeader.Visibility);

                    // Contextual headers are ordered after ordinary tabs.
                    Assert.Same(pictureTab, ribbon.TabHeaders.Last().Tab);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Minimize_And_Collapse_Are_Independent()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1000);
                try
                {
                    ribbon.IsMinimized = true;
                    Pump();
                    Assert.False(ribbon.IsMinimizedPopupOpen);

                    ribbon.SelectTab(Tab(ribbon, "Insert"), openWhenMinimized: true);
                    Pump();
                    Assert.True(ribbon.IsMinimizedPopupOpen);
                    ribbon.CloseTransientSurfaces();
                    Assert.False(ribbon.IsMinimizedPopupOpen);

                    // Crossing the size threshold collapses without overwriting the minimized preference.
                    Resize(window, 200);
                    Assert.True(ribbon.IsCollapsed);
                    Assert.True(ribbon.IsMinimized);

                    Resize(window, 1000);
                    Assert.False(ribbon.IsCollapsed);
                    Assert.True(ribbon.IsMinimized);

                    ribbon.IsMinimizable = false;
                    Assert.False(ribbon.IsMinimized);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void RaiseMouse(UIElement target, RoutedEvent routedEvent)
        {
            target.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = routedEvent });
        }

        private static void Click(UIElement target)
        {
            RaiseMouse(target, UIElement.PreviewMouseDownEvent);
            RaiseMouse(target, UIElement.MouseDownEvent);
            RaiseMouse(target, UIElement.PreviewMouseUpEvent);
            RaiseMouse(target, UIElement.MouseUpEvent);
        }

        [Fact]
        public void Minimized_Popup_Stays_Open_Until_The_User_Clicks_Outside_Or_Presses_Escape()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1000);
                try
                {
                    ribbon.IsMinimized = true;
                    Pump();

                    // A self-dismissing popup would close on the mouse-up that follows the header's mouse-down.
                    var popup = (Popup)ribbon.Template.FindName("PART_MinimizedPopup", ribbon);
                    Assert.True(popup.StaysOpen);

                    var insert = Tab(ribbon, "Insert").TabHeader!;
                    Click(insert);
                    Pump();
                    Assert.True(ribbon.IsMinimizedPopupOpen);
                    Assert.Same(Tab(ribbon, "Insert"), ribbon.SelectedTab);

                    // Another tab header switches tabs and keeps the popup open.
                    var home = Tab(ribbon, "Home").TabHeader!;
                    Click(home);
                    Pump();
                    Assert.True(ribbon.IsMinimizedPopupOpen);
                    Assert.Same(Tab(ribbon, "Home"), ribbon.SelectedTab);

                    // A press anywhere else in the window dismisses it.
                    var document = ((Panel)window.Content).Children.OfType<TextBox>().Single();
                    RaiseMouse(document, UIElement.PreviewMouseDownEvent);
                    Assert.False(ribbon.IsMinimizedPopupOpen);

                    // Escape dismisses it while focus is still in the window.
                    Click(home);
                    Pump();
                    Assert.True(ribbon.IsMinimizedPopupOpen);
                    var source = PresentationSource.FromVisual(window)!;
                    window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, Environment.TickCount, Key.Escape) { RoutedEvent = Keyboard.KeyDownEvent });
                    Assert.False(ribbon.IsMinimizedPopupOpen);

                    // Clicking the selected tab's header toggles the popup closed.
                    Click(home);
                    Pump();
                    Assert.True(ribbon.IsMinimizedPopupOpen);
                    Click(home);
                    Assert.False(ribbon.IsMinimizedPopupOpen);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Quick_Access_Toolbar_Is_Keyed_Cancellable_And_Shares_State()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    ribbon.QuickAccessToolbarMode = QuickAccessToolbarMode.Hidden;
                    ribbon.QuickAccessToolbarItemAdding += (_, e) => e.Cancel = e.Key == "Cut";

                    Assert.False(ribbon.AddToQuickAccessToolbar("Cut"));
                    Assert.False(ribbon.AddToQuickAccessToolbar("DoesNotExist"));
                    Assert.True(ribbon.AddToQuickAccessToolbar("Painter"));
                    Assert.False(ribbon.AddToQuickAccessToolbar("Painter"));
                    Assert.Equal(new[] { "Painter" }, ribbon.QuickAccessToolbarKeys);

                    // Adding to a hidden toolbar reveals it.
                    Assert.Equal(QuickAccessToolbarMode.Visible, ribbon.QuickAccessToolbarMode);

                    var source = (RibbonToggleButton)ribbon.FindCommand("Painter")!;
                    var presentation = (RibbonToggleButton)ribbon.QuickAccessToolbarItems.Single();
                    Assert.NotSame(source, presentation);
                    Assert.Equal("Painter", RibbonItem.GetSourceKey(presentation));

                    source.IsChecked = true;
                    Assert.True(presentation.IsChecked);

                    // Activating the presentation runs the source's policy (toggles the shared state).
                    presentation.PerformClick();
                    Assert.False(source.IsChecked);
                    Assert.False(presentation.IsChecked);

                    // Moving the toolbar below the Ribbon reparents it without losing membership.
                    ribbon.QuickAccessToolbarLocation = QuickAccessToolbarLocation.Below;
                    window.UpdateLayout();
                    Assert.True(ribbon.QuickAccessToolbar.IsVisible);
                    Assert.Single(ribbon.QuickAccessToolbarItems);

                    Assert.True(ribbon.RemoveFromQuickAccessToolbar("Painter"));
                    Assert.Empty(ribbon.QuickAccessToolbarKeys);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Disabled_Commands_Never_Execute_From_Any_Presentation()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var executed = 0;
                    var canExecute = false;
                    var command = new RoutedCommand();
                    window.CommandBindings.Add(new CommandBinding(command, (_, _) => executed++, (_, e) => e.CanExecute = canExecute));

                    var cut = (RibbonButton)ribbon.FindCommand("Cut")!;
                    cut.Command = command;
                    ribbon.AddToQuickAccessToolbar("Cut");
                    var qat = (RibbonButton)ribbon.QuickAccessToolbarItems.Single();
                    var overflow = (RibbonMenuItem)cut.CreatePresentation(RibbonPresentationKind.Overflow)!;

                    CommandManager.InvalidateRequerySuggested();
                    Pump();
                    Assert.False(cut.IsEnabled);
                    Assert.False(qat.IsEnabled);
                    Assert.False(overflow.IsEnabled);

                    cut.PerformClick();
                    qat.PerformClick();
                    overflow.PerformClick();
                    Assert.Equal(0, executed);

                    canExecute = true;
                    CommandManager.InvalidateRequerySuggested();
                    Pump();
                    Assert.True(qat.IsEnabled);

                    qat.PerformClick();
                    overflow.PerformClick();
                    Assert.Equal(2, executed);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Split_Button_Drop_Down_Does_Not_Invoke_The_Primary_Action()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var paste = (RibbonSplitButton)ribbon.FindCommand("Paste")!;
                    var clicks = 0;
                    paste.Click += (_, _) => clicks++;

                    paste.IsDropDownOpen = true;
                    Pump();
                    paste.IsDropDownOpen = false;
                    Assert.Equal(0, clicks);

                    paste.PerformClick();
                    Assert.Equal(1, clicks);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Backstage_Opens_Over_The_Host_And_Restores_Focus()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1000);
                try
                {
                    window.Activate();
                    var document = ((RibbonContainerPanel)window.Content).Children.OfType<TextBox>().Single();
                    document.Focus();

                    ribbon.OpenBackstage();
                    Pump();
                    Assert.True(ribbon.IsBackstageOpen);
                    Assert.True(ribbon.Backstage!.IsVisible);
                    Assert.Equal(0, ribbon.Backstage.SelectedIndex);

                    ribbon.Backstage.Close(restoreFocus: true);
                    Pump();
                    Assert.False(ribbon.IsBackstageOpen);
                    Assert.False(ribbon.Backstage.IsVisible);
                    if (document.IsKeyboardFocusWithin == false && window.IsActive)
                    {
                        Assert.Fail("Focus was not restored to the document.");
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Preferences_Round_Trip_And_Recover_From_Bad_Input()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                    ribbon.Density = UserInterfaceDensity.Compact;
                    ribbon.IsMinimized = true;
                    ribbon.QuickAccessToolbarLocation = QuickAccessToolbarLocation.Below;
                    ribbon.QuickAccessToolbarShowLabels = true;
                    ribbon.AddToQuickAccessToolbar("Copy");
                    ribbon.AddToQuickAccessToolbar("Bold");

                    var xml = RibbonSerializer.Serialize(ribbon);

                    var (window2, restored) = Host(1200);
                    try
                    {
                        var result = RibbonSerializer.Deserialize(restored, xml);
                        Assert.True(result.Success);
                        Assert.Empty(result.Diagnostics);
                        Assert.Equal(RibbonLayoutMode.Simplified, restored.LayoutMode);
                        Assert.Equal(UserInterfaceDensity.Compact, restored.Density);
                        Assert.True(restored.IsMinimized);
                        Assert.Equal(QuickAccessToolbarLocation.Below, restored.QuickAccessToolbarLocation);
                        Assert.True(restored.QuickAccessToolbarShowLabels);
                        Assert.Equal(new[] { "Copy", "Bold" }, restored.QuickAccessToolbarKeys);

                        // Malformed input changes nothing.
                        var malformed = RibbonSerializer.Deserialize(restored, "<RibbonPreferences version=\"1\"><LayoutMode>");
                        Assert.False(malformed.Success);
                        Assert.Equal(RibbonLayoutMode.Simplified, restored.LayoutMode);

                        // Newer schema versions are ignored rather than half-applied.
                        var newer = RibbonSerializer.Deserialize(restored, "<RibbonPreferences version=\"99\"><LayoutMode>Classic</LayoutMode></RibbonPreferences>");
                        Assert.False(newer.Success);
                        Assert.Equal(RibbonLayoutMode.Simplified, restored.LayoutMode);

                        // Invalid values and unknown/duplicate commands are skipped with diagnostics.
                        var partial = RibbonSerializer.Deserialize(restored, """
                            <RibbonPreferences version="1">
                              <LayoutMode>Sideways</LayoutMode>
                              <Density>Spacious</Density>
                              <QuickAccessToolbar><Items><Command key="Find" /><Command key="Find" /><Command key="Gone" /></Items></QuickAccessToolbar>
                            </RibbonPreferences>
                            """);
                        Assert.True(partial.Success);
                        Assert.Equal(RibbonLayoutMode.Simplified, restored.LayoutMode);
                        Assert.Equal(UserInterfaceDensity.Spacious, restored.Density);
                        Assert.Equal(new[] { "Find" }, restored.QuickAccessToolbarKeys);
                        Assert.Equal(3, partial.Diagnostics.Count);
                    }
                    finally
                    {
                        window2.Close();
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Duplicate_Keys_Are_Diagnosed()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    Assert.Empty(ribbon.ValidateKeys());
                    var group = Groups(Tab(ribbon, "Insert")).Single();
                    group.Items.Add(new RibbonButton { Key = "Cut", Label = "Another Cut" });
                    Assert.Single(ribbon.ValidateKeys());
                    Assert.IsType<RibbonButton>(ribbon.FindCommand("Cut"));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Generated_Key_Tips_Are_Unique_And_Prefix_Free()
        {
            var targets = new List<(string?, string?, bool)>
            {
                ("Paste", "V", false),
                ("Copy", null, false),
                ("Cut", null, false),
                ("Center", null, false),
                ("Format Painter", "FP", false),
                ("Font", null, false),
                ("Find", null, false),
                ("Save", null, true),
                ("Undo", null, true),
                ("Collides", "V", false),
                (null, null, false)
            };

            var diagnostics = new List<string>();
            var tips = RibbonKeyTipService.GenerateKeyTips(targets, diagnostics);

            Assert.Equal(targets.Count, tips.Count);
            Assert.All(tips, t => Assert.False(string.IsNullOrEmpty(t)));
            Assert.Equal(tips.Count, tips.Distinct().Count());
            foreach (var a in tips)
            {
                foreach (var b in tips)
                {
                    if (!ReferenceEquals(a, b) && a != b)
                    {
                        Assert.False(b.StartsWith(a, StringComparison.Ordinal), $"'{a}' is a prefix of '{b}'");
                    }
                }
            }

            Assert.Equal("V", tips[0]);
            Assert.Equal("FP", tips[4]);
            Assert.Equal("1", tips[7]);
            Assert.Equal("2", tips[8]);
            Assert.Single(diagnostics);
        }

        [Fact]
        public void External_Qat_Host_Preserves_Commands_KeyTips_And_Reparenting()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var panel = (Panel)window.Content;
                    var host = new ContentControl();
                    panel.Children.Add(host);
                    ribbon.QuickAccessToolbarHost = host;
                    ribbon.AddToQuickAccessToolbar("Painter");
                    Pump();
                    Assert.Same(ribbon.QuickAccessToolbar, host.Content);
                    Assert.Same(host, LogicalTreeHelper.GetParent(ribbon.QuickAccessToolbar));

                    var source = (RibbonToggleButton)ribbon.FindCommand("Painter")!;
                    var presentation = (RibbonToggleButton)ribbon.QuickAccessToolbarItems.Single();
                    presentation.PerformClick();
                    Assert.True(source.IsChecked);

                    // Root key tips must also include the toolbar outside the Ribbon's visual tree.
                    var inputSource = PresentationSource.FromVisual(window)!;
                    foreach (var key in new[] { Key.F10, Key.D1 })
                    {
                        window.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, inputSource, 0, key)
                        {
                            RoutedEvent = Keyboard.PreviewKeyDownEvent
                        });
                    }
                    Assert.False(source.IsChecked);

                    ribbon.OnApplyTemplate();
                    Pump();
                    Assert.Same(ribbon.QuickAccessToolbar, host.Content);
                    ribbon.QuickAccessToolbarLocation = QuickAccessToolbarLocation.Below;
                    Pump();
                    Assert.Null(host.Content);
                    Assert.True(ribbon.IsAncestorOf(ribbon.QuickAccessToolbar));

                    ribbon.QuickAccessToolbarLocation = QuickAccessToolbarLocation.Above;
                    var replacement = new ContentControl();
                    panel.Children.Add(replacement);
                    ribbon.QuickAccessToolbarHost = replacement;
                    Pump();
                    Assert.Null(host.Content);
                    Assert.Same(ribbon.QuickAccessToolbar, replacement.Content);

                    ribbon.QuickAccessToolbarHost = null;
                    Pump();
                    Assert.Null(replacement.Content);
                    Assert.Same(ribbon, LogicalTreeHelper.GetParent(ribbon.QuickAccessToolbar));
                    Assert.True(ribbon.IsAncestorOf(ribbon.QuickAccessToolbar));
                    Assert.Single(ribbon.QuickAccessToolbarItems);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static void PressEnter(UIElement target)
        {
            var source = PresentationSource.FromVisual(target)!;
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter) { RoutedEvent = Keyboard.KeyDownEvent });
        }

        [Fact]
        public void Invalid_Property_Values_Are_Rejected_When_Set()
        {
            RunSta(() =>
            {
                var ribbon = new Ribbon();
                Assert.Throws<ArgumentException>(() => ribbon.LayoutMode = (RibbonLayoutMode)42);
                Assert.Throws<ArgumentException>(() => ribbon.TabIndicatorAnimation = (RibbonTabIndicatorAnimation)(-1));
                Assert.Throws<ArgumentException>(() => ribbon.QuickAccessToolbarMode = (QuickAccessToolbarMode)9);
                Assert.Throws<ArgumentException>(() => ribbon.CollapseThresholdSize = Size.Empty);

                var button = new RibbonButton();
                Assert.Throws<ArgumentException>(() => button.PreferredVariant = (VariantSize)7);
                Assert.Throws<ArgumentException>(() => RibbonItem.SetCollapseBehavior(button, (ItemCollapseBehavior)5));

                var gallery = new RibbonGallery();
                Assert.Throws<ArgumentException>(() => gallery.ItemWidth = double.NaN);
                Assert.Throws<ArgumentException>(() => gallery.ItemHeight = 0);
                Assert.Throws<ArgumentException>(() => gallery.InlineColumns = 0);
                gallery.MaxDropDownHeight = double.PositiveInfinity;

                var combo = new RibbonComboBox { InputWidth = double.NaN };
                Assert.Throws<ArgumentException>(() => combo.InputWidth = -1);
                Assert.Throws<ArgumentException>(() => new RibbonTextBox().InputWidth = double.PositiveInfinity);

                var backstage = new RibbonBackstage();
                Assert.Throws<ArgumentException>(() => backstage.OpenDuration = TimeSpan.FromMilliseconds(-1));

                // Valid values still work and leave state intact.
                ribbon.LayoutMode = RibbonLayoutMode.Simplified;
                Assert.Equal(RibbonLayoutMode.Simplified, ribbon.LayoutMode);
            });
        }

        [Fact]
        public void Public_Methods_Validate_Their_Arguments()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    Assert.Throws<ArgumentNullException>(() => ribbon.AddToQuickAccessToolbar(null!));
                    Assert.Throws<ArgumentNullException>(() => ribbon.RemoveFromQuickAccessToolbar(null!));
                    Assert.Throws<ArgumentNullException>(() => ribbon.FindCommand(null!));
                    Assert.Throws<ArgumentNullException>(() => ribbon.FindTab(null!));
                    Assert.Throws<ArgumentNullException>(() => ribbon.SelectTab((RibbonTabItem)null!));
                    Assert.Throws<ArgumentException>(() => ribbon.SelectTab(new RibbonTabItem()));
                    Assert.Throws<ArgumentException>(() => RibbonSerializer.SaveToFile(ribbon, " "));

                    // Selecting by key honors visibility: a contextual tab whose context is inactive is not selectable.
                    Assert.True(ribbon.SelectTab("Insert"));
                    Assert.Equal("Insert", ribbon.SelectedTab?.Key);
                    Assert.False(ribbon.SelectTab("PictureFormat"));
                    Assert.False(ribbon.SelectTab("Nope"));
                    Assert.Equal("Insert", ribbon.SelectedTab?.Key);

                    // A replacement QAT presentation that already has a parent is refused.
                    ribbon.QuickAccessToolbarItemAdding += (_, e) =>
                        Assert.Throws<ArgumentException>(() => e.Presentation = ((RibbonContainerPanel)window.Content).Children.OfType<TextBox>().Single());
                    Assert.True(ribbon.AddToQuickAccessToolbar("Copy"));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Quick_Access_Toolbar_Honors_Opt_Out_And_Reports_Membership()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var cut = (RibbonButton)ribbon.FindCommand("Cut")!;
                    RibbonItem.SetCanAddToQuickAccessToolbar(cut, false);
                    Assert.False(ribbon.CanAddToQuickAccessToolbar("Cut"));
                    Assert.False(ribbon.AddToQuickAccessToolbar(cut));

                    var copy = (RibbonButton)ribbon.FindCommand("Copy")!;
                    Assert.False(RibbonItem.GetIsInQuickAccessToolbar(copy));
                    Assert.True(ribbon.AddToQuickAccessToolbar(copy));
                    Assert.True(RibbonItem.GetIsInQuickAccessToolbar(copy));
                    Assert.True(ribbon.RemoveFromQuickAccessToolbar("Copy"));
                    Assert.False(RibbonItem.GetIsInQuickAccessToolbar(copy));

                    // Restored preferences skip opted-out commands too.
                    var result = RibbonSerializer.Deserialize(ribbon, """
                        <RibbonPreferences version="1"><QuickAccessToolbar><Items><Command key="Cut" /><Command key="Copy" /></Items></QuickAccessToolbar></RibbonPreferences>
                        """);
                    Assert.Equal(new[] { "Copy" }, ribbon.QuickAccessToolbarKeys);
                    Assert.Single(result.Diagnostics);
                    Assert.True(RibbonItem.GetIsInQuickAccessToolbar(copy));

                    // Generated presentations follow a relabeled source for automation.
                    var presentation = ribbon.QuickAccessToolbarItems.Single();
                    copy.Label = "Duplicate";
                    Assert.Equal("Duplicate", System.Windows.Automation.AutomationProperties.GetName(presentation));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Checkable_Split_Button_Toggles_Before_Its_Command_On_Every_Surface()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var paste = (RibbonSplitButton)ribbon.FindCommand("Paste")!;
                    bool? seen = null;
                    paste.Command = new TestCommand(_ => seen = paste.IsChecked);

                    // Not checkable: the state never changes.
                    paste.PerformClick();
                    Assert.False(paste.IsChecked);

                    paste.IsCheckable = true;
                    paste.PerformClick();
                    Assert.True(paste.IsChecked);
                    Assert.True(seen);

                    Assert.True(ribbon.AddToQuickAccessToolbar("Paste"));
                    var qat = Assert.IsType<RibbonToggleButton>(ribbon.QuickAccessToolbarItems.Single());
                    Assert.True(qat.IsChecked);
                    qat.PerformClick();
                    Assert.False(paste.IsChecked);
                    Assert.False(qat.IsChecked);

                    var peer = (RibbonSplitButtonAutomationPeer)System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(paste);
                    Assert.NotNull(peer.GetPattern(System.Windows.Automation.Peers.PatternInterface.Toggle));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Gallery_And_Text_Box_Commands_Run_On_User_Commit_Only()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var gallery = (RibbonGallery)ribbon.FindCommand("Styles")!;
                    gallery.ItemsSource = new[] { "Normal", "Heading 1", "Title" };
                    var picked = new List<object?>();
                    gallery.Command = new TestCommand(picked.Add);
                    Pump();

                    // Programmatic selection never executes the command.
                    gallery.SelectedItem = "Title";
                    Assert.Empty(picked);

                    PressEnter(gallery);
                    Assert.Equal(new object?[] { "Title" }, picked);

                    gallery.CommandParameter = "fixed";
                    PressEnter(gallery);
                    Assert.Equal("fixed", picked[^1]);

                    var search = new RibbonTextBox { Key = "Search", Label = "Search" };
                    Groups(Tab(ribbon, "Home")).Last().Items.Add(search);
                    Pump();
                    var queries = new List<object?>();
                    search.Command = new TestCommand(queries.Add);
                    search.Text = "ribbon";
                    PressEnter(search);
                    Assert.Equal(new object?[] { "ribbon" }, queries);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void Preferences_Restore_The_Selected_Tab_And_Reject_Unsafe_Input()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                var directory = Path.Combine(Path.GetTempPath(), "MosaicRibbonTests", Guid.NewGuid().ToString("N"));
                try
                {
                    ribbon.SelectTab("Insert");
                    var path = Path.Combine(directory, "ribbon.xml");
                    RibbonSerializer.SaveToFile(ribbon, path);
                    RibbonSerializer.SaveToFile(ribbon, path); // Replaces atomically.
                    Assert.Single(Directory.GetFiles(directory));

                    ribbon.SelectTab("Home");
                    var loaded = RibbonSerializer.LoadFromFile(ribbon, path);
                    Assert.True(loaded.Success);
                    Assert.Empty(loaded.Diagnostics);
                    Assert.Equal("Insert", ribbon.SelectedTab?.Key);

                    // A tab that cannot be selected is reported, not forced.
                    var hidden = RibbonSerializer.Deserialize(ribbon, """<RibbonPreferences version="1"><SelectedTab key="PictureFormat" /></RibbonPreferences>""");
                    Assert.True(hidden.Success);
                    Assert.Single(hidden.Diagnostics);
                    Assert.Equal("Insert", ribbon.SelectedTab?.Key);

                    // DTDs (and so entity expansion) are refused.
                    var dtd = RibbonSerializer.Deserialize(ribbon, """
                        <?xml version="1.0"?>
                        <!DOCTYPE RibbonPreferences [ <!ENTITY big "Simplified"> ]>
                        <RibbonPreferences version="1"><LayoutMode>&big;</LayoutMode></RibbonPreferences>
                        """);
                    Assert.False(dtd.Success);
                    Assert.Equal(RibbonLayoutMode.Classic, ribbon.LayoutMode);

                    var huge = RibbonSerializer.Deserialize(ribbon, new string(' ', RibbonSerializer.MaxDocumentLength + 1));
                    Assert.False(huge.Success);

                    var missing = RibbonSerializer.LoadFromFile(ribbon, Path.Combine(directory, "missing.xml"));
                    Assert.False(missing.Success);
                }
                finally
                {
                    window.Close();
                    if (Directory.Exists(directory))
                    {
                        Directory.Delete(directory, recursive: true);
                    }
                }
            });
        }

        [Fact]
        public void Data_Bound_Menus_Mirror_Their_Items_And_Stay_Open_When_Asked()
        {
            RunSta(() =>
            {
                var (window, ribbon) = Host(1200);
                try
                {
                    var opened = new List<object?>();
                    var style = new Style(typeof(RibbonMenuItem));
                    style.Setters.Add(new Setter(HeaderedItemsControl.HeaderProperty, new System.Windows.Data.Binding()));
                    style.Setters.Add(new Setter(MenuItem.CommandProperty, new TestCommand(opened.Add)));
                    style.Setters.Add(new Setter(MenuItem.CommandParameterProperty, new System.Windows.Data.Binding()));

                    var recent = new RibbonMenuButton { Key = "Recent", Label = "Recent", ItemContainerStyle = style, ItemsSource = new[] { "a.txt", "b.txt" } };
                    Groups(Tab(ribbon, "Insert")).Single().Items.Add(recent);
                    Pump();

                    // Overflow and Quick Access Toolbar copies list the data items, styled like the original.
                    var overflow = (RibbonMenuItem)recent.CreatePresentation(RibbonPresentationKind.Overflow)!;
                    Assert.Equal(new object[] { "a.txt", "b.txt" }, overflow.Items.Cast<RibbonMenuItem>().Select(i => i.Header));
                    ((RibbonMenuItem)overflow.Items[1]).PerformClick();
                    Pump();
                    Assert.Equal(new object?[] { "b.txt" }, opened);

                    // A StaysOpenOnClick item does not dismiss its drop-down through CommandInvoked.
                    var paste = (RibbonSplitButton)ribbon.FindCommand("Paste")!;
                    var keepOpen = (RibbonMenuItem)paste.Items[0];
                    keepOpen.StaysOpenOnClick = true;
                    paste.IsDropDownOpen = true;
                    Pump();
                    keepOpen.PerformClick();
                    Pump();
                    Assert.True(paste.IsDropDownOpen);
                    keepOpen.StaysOpenOnClick = false;
                    keepOpen.PerformClick();
                    Pump();
                    Assert.False(paste.IsDropDownOpen);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private sealed class TestCommand : ICommand
        {
            private readonly Action<object?> _execute;

            public TestCommand(Action<object?> execute) => _execute = execute;

            public event EventHandler? CanExecuteChanged { add { } remove { } }

            public bool CanExecute(object? parameter) => true;

            public void Execute(object? parameter) => _execute(parameter);
        }
    }
}
