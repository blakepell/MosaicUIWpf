/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Tasks;
using Mosaic.UI.Wpf.Controls;
using Mosaic.UI.Wpf.Themes;
using MessageBox = Mosaic.UI.Wpf.Controls.MessageBox;

namespace Mosaic.UI.Wpf.Scripting.ScriptCommands;

/// <summary>
/// Provides Mosaic dialogs, pauses and keyboard automation to scripts.
/// </summary>
[ScriptModule(Name = "ui")]
public class UiScriptCommands
{
    /// <summary>
    /// Displays a themed alert on the application dispatcher.
    /// </summary>
    public void Alert(string message) => OnUi(() => MessageBox.Show(message, "Alert"));

    /// <summary>
    /// Displays a themed input dialog and returns accepted text, or an empty string on cancel.
    /// </summary>
    public string InputBox(string message) => OnUi(() => ShowTextDialog(message, false));

    /// <summary>
    /// Displays text in a themed read-only editor.
    /// </summary>
    public void ShowString(string text) => OnUi(() => ShowTextDialog(text, true));

    /// <summary>
    /// Pauses asynchronously for the specified number of milliseconds.
    /// </summary>
    public Task PauseAsync(int milliseconds) => Task.Delay(milliseconds);

    /// <summary>
    /// Pauses the script worker for the specified number of milliseconds.
    /// </summary>
    public void Pause(int milliseconds) => Thread.Sleep(milliseconds);

    /// <summary>
    /// Sends keystrokes using Windows Forms SendKeys syntax.
    /// </summary>
    public void SendKeys(string keys) => OnUi(() => { System.Windows.Forms.SendKeys.SendWait(keys); return true; });

    /// <summary>
    /// Requests garbage collection.
    /// </summary>
    public void GarbageCollect() => GC.Collect();

    private static T OnUi<T>(Func<T> action)
    {
        var dispatcher = Application.Current?.Dispatcher ?? throw new InvalidOperationException("UI commands require a WPF Application.");
        return dispatcher.Invoke(action);
    }

    private static string ShowTextDialog(string text, bool readOnly)
    {
        var window = new Window { Title = readOnly ? "Script output" : "Input", Width = 560, Height = 340,
            Owner = Application.Current.MainWindow, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        window.SetResourceReference(Control.BackgroundProperty, MosaicTheme.WindowBackgroundBrush);
        window.SetResourceReference(Control.ForegroundProperty, MosaicTheme.ControlTextForegroundBrush);
        foreach (string resource in new[] { "TextBox", "Button", "ScrollBar", "ScrollViewer" })
            window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri($"/Mosaic.UI.Wpf;component/Themes/Native/{resource}.xaml", UriKind.Relative) });
        var panel = new DockPanel { Margin = new Thickness(12) };
        var button = new Button { Content = readOnly ? "Close" : "OK", IsDefault = true, MinWidth = 80,
            HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 8, 0, 0) };
        button.Click += (_, _) => window.DialogResult = true;
        DockPanel.SetDock(button, Dock.Bottom);
        panel.Children.Add(button);
        if (!readOnly)
        {
            var prompt = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
            DockPanel.SetDock(prompt, Dock.Top);
            panel.Children.Add(prompt);
        }
        var editor = new TextBox { Text = readOnly ? text : "", IsReadOnly = readOnly, AcceptsReturn = readOnly,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto, TextWrapping = TextWrapping.Wrap };
        panel.Children.Add(editor);
        window.Content = panel;
        window.Loaded += (_, _) => editor.Focus();
        window.PreviewKeyDown += (_, e) => { if (e.Key == Key.Escape) window.Close(); };
        return window.ShowDialog() == true ? editor.Text : string.Empty;
    }
}