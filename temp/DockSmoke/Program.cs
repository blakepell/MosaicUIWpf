using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Mosaic.UI.Wpf.AvalonDock;
using Mosaic.UI.Wpf.AvalonDock.Controls;
using Mosaic.UI.Wpf.AvalonDock.Layout;
using Mosaic.UI.Wpf.AvalonDock.Themes;
using Mosaic.UI.Wpf.AvalonDock.Themes.VisualStudio;
class Program
{
    static void Check(bool condition, string description)
    {
        if (!condition) throw new Exception(description);
        Console.WriteLine("PASS: " + description);
    }
    static void Invoke(LayoutAnchorControl tab, string method, object args) => typeof(LayoutAnchorControl).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(tab, new[] { args });
    static void Click(LayoutAnchorControl tab) => Invoke(tab, "OnMouseDown", new MouseButtonEventArgs(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left) { RoutedEvent = Mouse.MouseDownEvent });
    [STAThread]
    static void Main()
    {
        var app = new Application();
        var root = new LayoutRoot { RootPanel = new LayoutPanel(new LayoutDocumentPane()) };
        var group = new LayoutAnchorGroup();
        var first = new LayoutAnchorable { Title = "Settings", Content = new TextBox(), CanShowOnHover = true };
        var second = new LayoutAnchorable { Title = "Other", Content = new TextBox() };
        group.Children.Add(first);
        group.Children.Add(second);
        root.LeftSide.Children.Add(group);
        var dock = new DockingManager { Layout = root, Theme = new MosaicTheme() };
        var window = new Window { Content = dock, Width = 600, Height = 400, Left = -10000, Top = -10000, ShowActivated = false, ShowInTaskbar = false };
        window.Show();
        window.UpdateLayout();
        var constructor = typeof(LayoutAnchorControl).GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic).Single();
        var tab = (LayoutAnchorControl)constructor.Invoke(new object[] { first });
        var other = (LayoutAnchorControl)constructor.Invoke(new object[] { second });
        Click(tab);
        Check(dock.AutoHideWindow.Model == first && first.IsActive, "First click opens and activates Settings");
        Invoke(tab, "OnMouseEnter", new MouseEventArgs(Mouse.PrimaryDevice, Environment.TickCount) { RoutedEvent = Mouse.MouseEnterEvent });
        Click(tab);
        Check(dock.AutoHideWindow.Model == null && !first.IsActive, "Second click collapses and deactivates Settings");
        Check(typeof(LayoutAnchorControl).GetField("_openUpTimer", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(tab) == null, "Collapse cancels pending hover timer");
        Click(tab);
        Check(dock.AutoHideWindow.Model == first, "Third click reopens Settings");
        Click(other);
        Check(dock.AutoHideWindow.Model == second, "Clicking another tab switches the pane");
        Click(other);
        Check(dock.AutoHideWindow.Model == null, "Switched pane also collapses on second click");
        foreach (var color in new[] { Colors.White, Color.FromRgb(30,30,30), Colors.DarkBlue })
        {
            dock.Resources[Mosaic.UI.Wpf.Themes.MosaicTheme.WindowBackgroundColor] = color;
            var tabBrush = (SolidColorBrush)dock.FindResource(ResourceKeys.AutoHideTabDefaultBackground);
            var dockBrush = (SolidColorBrush)dock.FindResource(ResourceKeys.Background);
            Check(tabBrush.Color == dockBrush.Color && tabBrush.Color == color, "Auto-hide and dock backgrounds track theme token " + color);
        }
        window.Close();
        app.Shutdown();
    }
}
