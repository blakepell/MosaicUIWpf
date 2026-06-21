/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

namespace MosaicWpfDemo.Views.Examples
{
    public partial class MarkdownEditorExample
    {
        private const string SampleMarkdown = """
        # Mosaic Markdown Editor

        A themed markdown editor powered by **AvalonEdit**.

        ## Features

        - Formatting toolbar (**bold**, _italic_, ~~strikethrough~~, `inline code`)
        - Headings, lists, block quotes, links, and tables
        - List continuation on **Enter** and indent with **Tab**

        > Tip: Right-click to *Copy as HTML* or *Paste Image as Base64*.

        | Feature | Supported |
        | --- | --- |
        | Syntax highlighting | Yes |
        | Theming | Yes |
        """;

        public MarkdownEditorExample()
        {
            InitializeComponent();

            this.Editor.Text = SampleMarkdown;
            this.Editor.FileName = "Sample.md";
            this.Editor.IsModified = false;
        }
    }
}
