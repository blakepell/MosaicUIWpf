/*
 * Mosaic UI for WPF
 * @project lead      : Blake Pell
 * @license           : MIT - https://opensource.org/license/mit/
 */

using ICSharpCode.AvalonEdit.CodeCompletion;
using Mosaic.UI.Wpf.Scripting;
using Mosaic.UI.Wpf.Themes;

namespace Mosaic.UI.Wpf.Controls;

/// <summary>Schema metadata rendered with the script editor's completion popup chrome.</summary>
internal sealed class SqliteCompletionData(string text, string kind, string detail, string summary, double priority = 1.0)
    : SyntaxCompletionData(text, new ScriptCompletionDescription(kind == "PrimaryKey" ? "Primary key" : kind,
        kind is "Table" or "View" ? string.Empty : detail, summary), priority: priority)
{
    public string Kind => kind;
    public string Detail => detail;

    internal static void ConfigureWindow(CompletionWindow window, MosaicThemeMode theme)
    {
        ScriptEditorSupport.AddThemeResources(window, theme);
        window.Resources.MergedDictionaries.Add(new ResourceDictionary
        {
            Source = new Uri("/Mosaic.UI.Wpf;component/Controls/SqliteQueryControl/SqliteCompletionWindow.xaml", UriKind.Relative)
        });
        window.CompletionList.Style = (Style)window.FindResource("SqliteCompletionListStyle");
        window.Width = 300;
        window.ResizeMode = ResizeMode.NoResize;
        window.BorderThickness = new Thickness(1);
    }
}
