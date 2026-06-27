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

using System.Windows.Media.Imaging;

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// Represents a single row displayed by the <see cref="Files"/> control. A row describes either a
    /// file or — when folder navigation is enabled — a sub-folder (or the <c>..</c> parent-folder entry).
    /// Exposes the name, last-modified time, size, and the icon associated with the entry. The full path
    /// is retained so consumers can resolve a file via <see cref="FileInfo"/> or navigate into a folder.
    /// </summary>
    public sealed class FileItem : ObservableObject
    {
        /// <summary>
        /// The display name used for the parent-folder navigation entry.
        /// </summary>
        internal const string ParentDirectoryName = "..";

        private static ImageSource? _folderIcon;

        private string _fullPath = string.Empty;
        private string _name = string.Empty;
        private DateTime _dateModified;
        private long _size;
        private ImageSource? _icon;
        private bool _isDirectory;
        private bool _isParentNavigation;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileItem"/> class from a <see cref="System.IO.FileInfo"/>.
        /// </summary>
        /// <param name="info">The file to describe.</param>
        public FileItem(FileInfo info)
        {
            this.Update(info);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileItem"/> class from a <see cref="System.IO.DirectoryInfo"/>.
        /// </summary>
        /// <param name="info">The directory to describe.</param>
        /// <param name="isParentNavigation">When <c>true</c>, this row represents the <c>..</c> parent-folder navigation entry.</param>
        public FileItem(DirectoryInfo info, bool isParentNavigation = false)
        {
            this.Update(info, isParentNavigation);
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
        /// Gets a culture-aware, human-friendly representation of <see cref="DateModified"/>. The
        /// parent-folder (<c>..</c>) navigation entry returns an empty string.
        /// </summary>
        public string DateModifiedDisplay => _isParentNavigation ? string.Empty : _dateModified.ToString("g");

        /// <summary>
        /// Gets a value indicating whether this row represents a folder (either a sub-folder or the
        /// <c>..</c> parent-folder navigation entry) rather than a file.
        /// </summary>
        public bool IsDirectory
        {
            get => _isDirectory;
            private set => SetProperty(ref _isDirectory, value);
        }

        /// <summary>
        /// Gets a value indicating whether this row is the <c>..</c> parent-folder navigation entry.
        /// </summary>
        public bool IsParentNavigation
        {
            get => _isParentNavigation;
            private set => SetProperty(ref _isParentNavigation, value);
        }

        /// <summary>
        /// Gets the ordering group used to keep folders above files (and the <c>..</c> entry at the very
        /// top) when the listing is sorted: <c>0</c> for the parent entry, <c>1</c> for folders, <c>2</c> for files.
        /// </summary>
        public int SortGroup => _isParentNavigation ? 0 : _isDirectory ? 1 : 2;

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
        /// Folders return an empty string because no size is shown for them.
        /// </summary>
        public string SizeDisplay => _isDirectory ? string.Empty : FormatSize(_size);

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
            this.IsParentNavigation = false;
            this.IsDirectory = false;
            this.FullPath = info.FullName;
            this.Name = info.Name;
            this.DateModified = info.LastWriteTime;
            this.Size = info.Exists ? info.Length : 0;
            this.Icon = FileIconHelper.GetIcon(info.FullName);
            OnPropertyChanged(nameof(SortGroup));
        }

        /// <summary>
        /// Refreshes this item's properties from the supplied <see cref="System.IO.DirectoryInfo"/>. Used by the
        /// <see cref="Files"/> control to display a sub-folder (or the <c>..</c> parent-folder navigation entry)
        /// when folder navigation is enabled.
        /// </summary>
        /// <param name="info">The current directory state.</param>
        /// <param name="isParentNavigation">When <c>true</c>, this row represents the <c>..</c> parent-folder navigation entry.</param>
        internal void Update(DirectoryInfo info, bool isParentNavigation)
        {
            this.IsParentNavigation = isParentNavigation;
            this.IsDirectory = true;
            this.FullPath = info.FullName;
            this.Name = isParentNavigation ? ParentDirectoryName : info.Name;
            this.DateModified = info.LastWriteTime;
            this.Size = 0;
            this.Icon = FolderIcon;
            OnPropertyChanged(nameof(SortGroup));
        }

        /// <summary>
        /// Gets the shared, frozen folder icon used for directory rows, loaded once from the library's
        /// embedded <c>folder2-open-48.png</c> asset.
        /// </summary>
        private static ImageSource? FolderIcon => _folderIcon ??= LoadFolderIcon();

        /// <summary>
        /// Loads and freezes the folder icon from the packaged asset. Returns <c>null</c> if the asset cannot be loaded.
        /// </summary>
        private static ImageSource? LoadFolderIcon()
        {
            try
            {
                var uri = new Uri("pack://application:,,,/Mosaic.UI.Wpf;component/Assets/Images/folder2-open-48.png", UriKind.Absolute);
                var image = new BitmapImage(uri);
                image.Freeze();
                return image;
            }
            catch
            {
                return null;
            }
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
