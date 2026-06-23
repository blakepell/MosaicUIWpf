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
    public partial class MarkdownViewerExample
    {
        public MarkdownViewerExample()
        {
            InitializeComponent();

            this.Viewer.Markdown = SampleMarkdown;
        }

        /// <summary>
        /// A sample document exercising headings, emphasis, lists, code, quotes, tables, and links.
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

            ## Table

            | Language | Typed | Year |
            | --- | --- | --- |
            | C#     | Yes  | 2000 |
            | Python | No   | 1991 |

            [Mosaic UI for WPF](https://github.com/blakepell/MosaicUIWpf)

            ## Image

            An inline base64 image: ![Image](data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAACAAAAAgCAMAAABEpIrGAAAAAXNSR0IArs4c6QAAAARnQU1BAACxjwv8YQUAAAC3UExURf///f7+/wJb/QBc/wBc/f3//ABb//////7+/v/+//7//QBd/wBb/gBd+wBd/QBa///9//7//wFd/wJb//+gwP+gvv+iwf+fwAFc//6hwAFa/v+hv/6fvf+gwv+fwv6gwgFd/ABe/wBc/P+hw/6fvwBd/ABb/P6hvv+ewv+ewQBc+v7/+/6gxP3+//+evQFd/gNc//yhwv+fvvz//wBf/v3//gBc+/+hwfugv/+gwf+gvP2gvwAAAMDH0YIAAAA9dFJOU////////////////////////////////////////////////////////////////////////////////wAJL6xfAAAACXBIWXMAAA7CAAAOwgEVKEqAAAACEklEQVQ4T2WTi3bbIAyGSUVRsF2nGNHEWxwvzbbs3t27rLz/c+3HUCfnTAdzQPqQhIRVhKhFmmO8Ik0YV3m3yLY0XU9zjIbIpC/v1PU0p4mXbCuum5uWIO1NU3NloZyBJVesLK3qDNQrsgqq5QzAx61ybQdHjLHsWqdup/MzULF1RKqyFYYicparbClAjBrpG+oIw2Cpi/oM1Cn8xVTUZ8CTCJGEu/VGr3vti/oMEIU1vRCRlwkzTVFnwJDp5G47kBHZja9IdttA+3u6ALo6BDk0FPpR6u61DH1KuQDmja7fdhsK2yMirMNxOIjRHrl2yCQBG282SLwfBjmEPvQSpMN9k5QQ7GQQQSfhXN6RR8Oaluy5F8oOQUZtyI8SRq3Joyk3i6mW2QMjq4CrkIEbgtXgPMptnz24IPKeyHd1qnOK/4HtR8uwZkApqxwZx9Yq6xatSWf7T/2hAM46Zibiz8opeKZ7RoIiX6QASahBiAdP+muLMC26Gfr+2yVgGk8e0Ved9igjQnw/9jOwQf7Mvsb96lXLP0D8zJYCpNeuGAUi3xiztOn1ZMsM4JHa1qTXYrBGolOrZmBP6pe19Nv4BgXFE2faZ8uc5PFx7A9D6otidqlEWebF7s/pUXaoCf4HvH0UKssMaGpQiRgH+YuGhVDUZ4CaB3QxxtOTjGs5PRX1GXiWevo1EprlPwClmCTvYvwH1MN9iHfX3PAAAAAASUVORK5CYII=)
            """;
    }
}
