/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Collections.ObjectModel;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class StringListEditorExample
    {
        /// <summary>
        /// Gets or sets the collection strings used for the string list editor example.
        /// </summary>
        public ObservableCollection<string> ControlList { get; set; } = new();

        public ObservableCollection<string> ControlList2 { get; set; } = new();

        public StringListEditorExample()
        {
            this.DataContext = this;
            InitializeComponent();

            StringListEditorWithValidation.Validate = Validate;
        }

        private bool Validate(string arg)
        {
            // Don't let in strings less than 3 characters.
            if (arg.Length < 3)
            {
                return false;
            }

            return true;
        }
    }
}