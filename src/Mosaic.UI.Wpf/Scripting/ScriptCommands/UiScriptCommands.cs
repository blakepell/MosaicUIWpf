/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Threading.Tasks;
using Mosaic.UI.Wpf.Controls;
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
    /// Displays a themed OK/Cancel confirmation and returns true when the user clicks OK.
    /// </summary>
    public bool Confirm(string message) => OnUi(() => MessageBox.Show(message, "Confirm", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK);

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
    public void SendKeys(string keys) => OnUi(() =>
    {
        System.Windows.Forms.SendKeys.SendWait(keys);
        return true;
    });

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
        var window = new ScriptTextWindow(text, readOnly)
        {
            Owner = Application.Current.MainWindow
        };

        return window.ShowDialog() == true ? window.Text : string.Empty;
    }
}