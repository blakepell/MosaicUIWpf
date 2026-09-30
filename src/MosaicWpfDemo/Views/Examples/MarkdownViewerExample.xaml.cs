/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Win32;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class MarkdownViewerExample
    {
        public MarkdownViewerExample()
        {
            InitializeComponent();

            this.Viewer.Markdown = SampleMarkdown;
        }

        /// <summary>
        /// The primary split button action: prompts for a file and saves as RTF or XPS based on the
        /// file type the user picks.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SaveAs("Rich Text Format (*.rtf)|*.rtf|XPS Document (*.xps)|*.xps", ".rtf");
        }

        private void SaveAsRtf_Click(object sender, RoutedEventArgs e)
        {
            SaveAs("Rich Text Format (*.rtf)|*.rtf", ".rtf");
        }

        private void SaveAsXps_Click(object sender, RoutedEventArgs e)
        {
            SaveAs("XPS Document (*.xps)|*.xps", ".xps");
        }

        /// <summary>
        /// Prints the rendered document. Choosing "Microsoft Print to PDF" in the print dialog
        /// produces a PDF.
        /// </summary>
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Viewer.Print(description: "Markdown Preview");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Print Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Prompts for a destination file and saves the viewer's contents in the format matching
        /// the chosen file's extension.
        /// </summary>
        /// <param name="filter">The save dialog's file type filter.</param>
        /// <param name="defaultExtension">The extension used when the user does not type one.</param>
        private void SaveAs(string filter, string defaultExtension)
        {
            var dialog = new SaveFileDialog
            {
                Title = "Save Markdown Document",
                FileName = "Markdown Preview",
                Filter = filter,
                DefaultExt = defaultExtension,
                AddExtension = true
            };

            if (dialog.ShowDialog(Window.GetWindow(this)) != true)
            {
                return;
            }

            try
            {
                if (string.Equals(Path.GetExtension(dialog.FileName), ".xps", StringComparison.OrdinalIgnoreCase))
                {
                    this.Viewer.SaveAsXps(dialog.FileName);
                }
                else
                {
                    this.Viewer.SaveAsRtf(dialog.FileName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Handles an @-prefixed event link such as <c>[Blake's Articles](@ShowArticle?keyword=bpell)</c>.
        /// The viewer parses the event name and the query string; this demo simply displays them.
        /// </summary>
        private void Viewer_EventRaised(object sender, MarkdownEventRaisedEventArgs e)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Event: {e.EventName}");
            sb.AppendLine($"Link: {e.Link}");
            sb.AppendLine();

            if (e.Parameters.Count == 0)
            {
                sb.AppendLine("No parameters were supplied.");
            }
            else
            {
                sb.AppendLine("Parameters:");

                foreach (var pair in e.Parameters)
                {
                    sb.AppendLine($"  {pair.Key} = {pair.Value}");
                }
            }

            MessageBox.Show(sb.ToString().TrimEnd(), "Markdown Event Link", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        /// <summary>
        /// A sample document exercising headings, emphasis, lists, code, quotes, tables, links, and
        /// @-prefixed event links.
        /// </summary>
        private const string SampleMarkdown =
            """
            # Markdown Preview

            This is **bold**, this is *italic*, and this is `inline code`.

            ## List

            - First item
            - Second item
            - Third item

            ## Numbered List

            1. One
            2. Two
            3. Three

            > This is a block quote.

            ```csharp
            public static void Main()
            {
                Console.WriteLine("Hello Markdown");
            }
            ```

            A fence without a language is shown in the same editor, unhighlighted:

            ```
            > dotnet build MosaicUIWpf.sln
            Build succeeded.
            ```

            ## Table

            | Language | Typed | Year |
            | --- | --- | --- |
            | C#     | Yes  | 2000 |
            | Python | No   | 1991 |

            [Mosaic UI for WPF](https://github.com/blakepell/MosaicUIWpf)

            ## Event Links

            A link whose destination starts with `@` raises the viewer's `EventRaised` event instead of
            navigating. The text after `@` is the event name and the query string becomes the parameters:

            - [Blake's Articles](@ShowArticle?keyword=bpell)
            - [Search two terms](@Search?term=wpf&term2=markdown&page=2)
            - [Refresh (no parameters)](@Refresh)

            ## Image

            An inline base64 image: ![Image](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAMAAABEpIrGAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAC3UExURf///f7+/wJb/QBc/wBc/f3//ABb//////7+/v/+//7//QBd/wBb/gBd+wBd/QBa///9//7//wFd/wJb//+gwP+gvv+iwf+fwAFc//6hwAFa/v+hv/6fvf+gwv+fwv6gwgFd/ABe/wBc/P+hw/6fvwBd/ABb/P6hvv+ewv+ewQBc+v7/+/6gxP3+//+evQFd/gNc//yhwv+fvvz//wBf/v3//gBc+/+hwfugv/+gwf+gvP2gvwAAAMDH0YIAAAA9dFJOU////////////////////////////////////////////////////////////////////////////////wAJL6xfAAAACXBIWXMAAA7CAAAOwgEVKEqAAAACEklEQVQ4T2WTi3bbIAyGSUVRsF2nGNHEWxwvzbbs3t27rLz/c+3HUCfnTAdzQPqQhIRVhKhFmmO8Ik0YV3m3yLY0XU9zjIbIpC/v1PU0p4mXbCuum5uWIO1NU3NloZyBJVesLK3qDNQrsgqq5QzAx61ybQdHjLHsWqdup/MzULF1RKqyFYYicparbClAjBrpG+oIw2Cpi/oM1Cn8xVTUZ8CTCJGEu/VGr3vti/oMEIU1vRCRlwkzTVFnwJDp5G47kBHZja9IdttA+3u6ALo6BDk0FPpR6u61DH1KuQDmja7fdhsK2yMirMNxOIjRHrl2yCQBG282SLwfBjmEPvQSpMN9k5QQ7GQQQSfhXN6RR8Oaluy5F8oOQUZtyI8SRq3Joyk3i6mW2QMjq4CrkIEbgtXgPMptnz24IPKeyHd1qnOK/4HtR8uwZkApqxwZx9Yq6xatSWf7T/2hAM46Zibiz8opeKZ7RoIiX6QASahBiAdP+muLMC26Gfr+2yVgGk8e0Ved9igjQnw/9jOwQf7Mvsb96lXLP0D8zJYCpNeuGAUi3xiztOn1ZMsM4JHa1qTXYrBGolOrZmBP6pe19Nv4BgXFE2faZ8uc5PFx7A9D6otidqlEWebF7s/pUXaoCf4HvH0UKssMaGpQiRgH+YuGhVDUZ4CaB3QxxtOTjGs5PRX1GXiWevo1EprlPwClmCTvYvwH1MN9iHfX3PAAAAAASUVORK5CYII=)
            """;
    }
}
