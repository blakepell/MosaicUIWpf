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
    /// Displays a toast notification over the main window.
    /// </summary>
    /// <param name="title">The bolded title line.</param>
    /// <param name="message">The message body.</param>
    /// <param name="severity">Success, Info, Warning or Error (case-insensitive).</param>
    /// <param name="durationMs">How long the toast stays open; zero or less keeps it open until closed.</param>
    /// <param name="quadrant">TopLeft, TopRight, BottomLeft or BottomRight (case-insensitive).</param>
    [ScriptModuleMethod(Name = nameof(ShowToast),
        Description = "Displays a toast notification. Severity: Success, Info, Warning, Error. Quadrant: TopLeft, TopRight, BottomLeft, BottomRight. A durationMs of 0 or less stays open until closed.",
        ParameterCount = 5)]
    public void ShowToast(string title, string message, string severity = "Info", int durationMs = 5000, string quadrant = "BottomRight")
    {
        var toastSeverity = ParseEnum<ToastSeverity>(severity, nameof(severity));
        var toastQuadrant = ParseEnum<ToastQuadrant>(quadrant, nameof(quadrant));
        TimeSpan? duration = durationMs > 0 ? TimeSpan.FromMilliseconds(durationMs) : null;

        OnUi(() =>
        {
            var manager = ToastManager.Default
                          ?? ToastManager.ForElement(Application.Current.MainWindow)
                          ?? throw new InvalidOperationException("No toast host is available; the main window must be loaded.");

            manager.Show(title, message, toastSeverity, duration, toastQuadrant);
            return true;
        });
    }

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

    private static T ParseEnum<T>(string value, string parameterName) where T : struct, Enum
    {
        return Enum.TryParse<T>(value, true, out var result) && Enum.IsDefined(result)
            ? result
            : throw new ArgumentException($"'{value}' is not valid; expected one of: {string.Join(", ", Enum.GetNames<T>())}.", parameterName);
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