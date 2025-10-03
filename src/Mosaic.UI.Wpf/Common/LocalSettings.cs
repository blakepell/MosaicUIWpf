/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Argus.IO;

namespace Mosaic.UI.Wpf.Common
{
    /// <summary>
    /// Minimal settings specific to a single workstation.
    /// </summary>
    [TypeConverter(typeof(ExpandableObjectConverter))]
    public partial class LocalSettings : ObservableObject
    {
        [property: Category("File System")]
        [property: DisplayName("Documents Folder")]
        [property: Description("The folder the app will use for user stored documents and settings.")]
        [property: ReadOnly(false)]
        [property: Browsable(true)]
        [ObservableProperty]
        private string? _documentsFolder;

        public override string ToString()
        {
            return DocumentsFolder ?? "";
        }

        /// <summary>
        /// Custom logic for when known properties change value.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(DocumentsFolder))
            {
                if (!Directory.Exists(this.DocumentsFolder))
                {
                    return;
                }

                if (AppServices.IsSingletonRegistered<JsonFileService>())
                {
                    var fs = AppServices.GetRequiredService<JsonFileService>();
                    fs.FolderPath = this.DocumentsFolder;
                }
            }
        }
    }
}