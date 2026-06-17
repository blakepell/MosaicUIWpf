/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single file row displayed by the <see cref="Files"/> control. Exposes the
    /// file name, last-modified time, size, and the operating-system shell icon associated with the
    /// file type. The full path is retained so consumers can resolve the file via <see cref="FileInfo"/>.
    /// </summary>
    public sealed class FileItem : ObservableObject
    {
        private string _fullPath = string.Empty;
        private string _name = string.Empty;
        private DateTime _dateModified;
        private long _size;
        private ImageSource? _icon;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileItem"/> class from a <see cref="System.IO.FileInfo"/>.
        /// </summary>
        /// <param name="info">The file to describe.</param>
        public FileItem(FileInfo info)
        {
            this.Update(info);
        }

        /// <summary>
        /// Gets the full path of the file on disk.
        /// </summary>
        public string FullPath
        {
            get => _fullPath;
            private set => SetProperty(ref _fullPath, value);
        }

        /// <summary>
        /// Gets the file name (without the directory portion of the path).
        /// </summary>
        public string Name
        {
            get => _name;
            private set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets the date and time the file was last written to.
        /// </summary>
        public DateTime DateModified
        {
            get => _dateModified;
            private set
            {
                if (SetProperty(ref _dateModified, value))
                {
                    OnPropertyChanged(nameof(DateModifiedDisplay));
                }
            }
        }

        /// <summary>
        /// Gets a culture-aware, human-friendly representation of <see cref="DateModified"/>.
        /// </summary>
        public string DateModifiedDisplay => _dateModified.ToString("g");

        /// <summary>
        /// Gets the size of the file, in bytes.
        /// </summary>
        public long Size
        {
            get => _size;
            private set
            {
                if (SetProperty(ref _size, value))
                {
                    OnPropertyChanged(nameof(SizeDisplay));
                }
            }
        }

        /// <summary>
        /// Gets a human-friendly representation of <see cref="Size"/> (for example <c>525 KB</c>, <c>2.1 MB</c>, or <c>1.1 GB</c>).
        /// </summary>
        public string SizeDisplay => FormatSize(_size);

        /// <summary>
        /// Gets the operating-system shell icon associated with this file's type.
        /// </summary>
        public ImageSource? Icon
        {
            get => _icon;
            private set => SetProperty(ref _icon, value);
        }

        /// <summary>
        /// Gets a fresh <see cref="System.IO.FileInfo"/> for the file represented by this item.
        /// </summary>
        public FileInfo FileInfo => new(_fullPath);

        /// <summary>
        /// Refreshes this item's properties from the supplied <see cref="System.IO.FileInfo"/>. Used by the
        /// <see cref="Files"/> control to update an existing row in place (preserving selection) when the
        /// underlying file changes.
        /// </summary>
        /// <param name="info">The current file state.</param>
        internal void Update(FileInfo info)
        {
            this.FullPath = info.FullName;
            this.Name = info.Name;
            this.DateModified = info.LastWriteTime;
            this.Size = info.Exists ? info.Length : 0;
            this.Icon = FileIconHelper.GetIcon(info.FullName);
        }

        /// <summary>
        /// Formats a byte count as a friendly, rounded string using binary (1024-based) units.
        /// </summary>
        /// <param name="bytes">The number of bytes.</param>
        /// <returns>A string such as <c>0 B</c>, <c>525 KB</c>, <c>2.1 MB</c>, or <c>1.1 GB</c>.</returns>
        public static string FormatSize(long bytes)
        {
            if (bytes < 0)
            {
                return string.Empty;
            }

            string[] units = { "B", "KB", "MB", "GB", "TB", "PB" };
            double size = bytes;
            int unit = 0;

            while (size >= 1024 && unit < units.Length - 1)
            {
                size /= 1024;
                unit++;
            }

            // Whole bytes have no decimal; larger units show one decimal place when not a whole number.
            string formatted = unit == 0 || size >= 100 || size == Math.Floor(size)
                ? size.ToString("0")
                : size.ToString("0.0");

            return $"{formatted} {units[unit]}";
        }
    }
}
