/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class FavoriteCheckBoxExample : INotifyPropertyChanged
    {
        private bool _isFavorite = true;
        private bool _isPinned;

        public FavoriteCheckBoxExample()
        {
            InitializeComponent();
            DataContext = this;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public bool IsFavorite
        {
            get => _isFavorite;
            set
            {
                if (_isFavorite == value)
                {
                    return;
                }

                _isFavorite = value;
                OnPropertyChanged();
            }
        }

        public bool IsPinned
        {
            get => _isPinned;
            set
            {
                if (_isPinned == value)
                {
                    return;
                }

                _isPinned = value;
                OnPropertyChanged();
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
