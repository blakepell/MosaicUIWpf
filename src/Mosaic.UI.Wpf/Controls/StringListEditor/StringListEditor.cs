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
using CommunityToolkit.Mvvm.Input;

// ReSharper disable CheckNamespace

namespace Mosaic.UI.Wpf.Controls
{
    /// <summary>
    /// A StringListEditor component.
    /// </summary>
    public class StringListEditor : ContentControl
    {
        /// <summary>
        /// Represents the TextBox control used for user input.
        /// </summary>
        public TextBox InputTextBox { get; private set; }

        /// <summary>
        /// Gets the <see cref="ListBox"/> used for displaying and managing the string list.
        /// </summary>
        public ListBox InputListBox { get; private set; }

        /// <summary>
        /// Handles deleting an item from the <see cref="StringListEditor"/>.
        /// </summary>
        public ICommand DeleteItemCommand { get; set; }

        /// <summary>
        /// Identifies the <see cref="Items"/> dependency property, which holds a collection of strings.
        /// </summary>
        public static readonly DependencyProperty ItemsProperty = DependencyProperty.Register(
            nameof(Items), typeof(ObservableCollection<string>), typeof(StringListEditor), new PropertyMetadata(default(ObservableCollection<string>)));

        /// <summary>
        /// Gets or sets the collection of strings.
        /// </summary>
        public ObservableCollection<string> Items
        {
            get => (ObservableCollection<string>)GetValue(ItemsProperty);
            set => SetValue(ItemsProperty, value);
        }

        /// <summary>
        /// Identifies the <see cref="AllowDuplicates"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllowDuplicatesProperty = DependencyProperty.Register(
            nameof(AllowDuplicates), typeof(bool), typeof(StringListEditor), new PropertyMetadata(false));

        /// <summary>
        /// If duplicates are allowed in the box.
        /// </summary>
        public bool AllowDuplicates
        {
            get => (bool)GetValue(AllowDuplicatesProperty);
            set => SetValue(AllowDuplicatesProperty, value);
        }

        /// <summary>
        /// The max length of any single item.
        /// </summary>
        public static readonly DependencyProperty MaxLengthProperty = DependencyProperty.Register(
            nameof(MaxLength), typeof(int), typeof(StringListEditor), new PropertyMetadata(int.MaxValue));

        /// <summary>
        /// The max length of any single item.
        /// </summary>
        public int MaxLength
        {
            get => (int)GetValue(MaxLengthProperty);
            set => SetValue(MaxLengthProperty, value);
        }

        /// <summary>
        /// The margin for the text box control of the string list editor.
        /// </summary>
        public static readonly DependencyProperty TextBoxMarginProperty = DependencyProperty.Register(
            nameof(TextBoxMargin), typeof(Thickness), typeof(StringListEditor), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// The margin for the text box control of the string list editor.
        /// </summary>
        public Thickness TextBoxMargin
        {
            get => (Thickness)GetValue(TextBoxMarginProperty);
            set => SetValue(TextBoxMarginProperty, value);
        }

        /// <summary>
        /// The margin for the list box control of the string list editor.
        /// </summary>
        public static readonly DependencyProperty ListBoxMarginProperty = DependencyProperty.Register(
            nameof(ListBoxMargin), typeof(Thickness), typeof(StringListEditor), new PropertyMetadata(default(Thickness)));

        /// <summary>
        /// The margin for the list box control of the string list editor.
        /// </summary>
        public Thickness ListBoxMargin
        {
            get => (Thickness)GetValue(ListBoxMarginProperty);
            set => SetValue(ListBoxMarginProperty, value);
        }

        /// <summary>
        /// A provided validation function.
        /// </summary>
        public Func<string, bool>? Validate { get; set; }

        /// <summary>
        /// Initializes the <see cref="StringListEditor"/> class and overrides the default style key metadata.
        /// </summary>
        static StringListEditor()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(StringListEditor), new FrameworkPropertyMetadata(typeof(StringListEditor)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringListEditor"/> class.
        /// </summary>
        public StringListEditor()
        {
            DeleteItemCommand = new RelayCommand<object>(this.DeleteItem);
        }

        /// <summary>
        /// Finds all of our controls that need to be wired up inside of this template.
        /// </summary>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (this.GetTemplateChild("ButtonAdd") is Button btn)
            {
                btn.Click += this.ButtonAdd_Click;
            }

            if (this.GetTemplateChild("ListItems") is ListBox lb)
            {
                this.InputListBox = lb;
                lb.PreviewKeyDown += ListItems_OnPreviewKeyDown;
            }

            if (this.GetTemplateChild("TextKeyword") is TextBox tb)
            {
                this.InputTextBox = tb;
                tb.PreviewKeyDown += TextKeyword_OnPreviewKeyDown;
            }
        }

        /// <summary>
        /// Called the delete item button has been clicked
        /// </summary>
        /// <param name="item"></param>
        public void DeleteItem(object? item)
        {
            if (item is not string buf)
            {
                return;
            }

            this.Items.Remove(buf);
        }

        /// <summary>
        /// Adds an item to the collection, performing validation and duplicate checks as necessary.
        /// </summary>
        public void AddItem(string? item)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                return;
            }

            // Trim the text
            InputTextBox.Text = InputTextBox.Text.Trim();

            // No empty items.
            if (string.IsNullOrWhiteSpace(InputTextBox.Text))
            {
                return;
            }

            // No duplicates if specified.
            if (!this.AllowDuplicates && this.Items.Contains(InputTextBox.Text))
            {
                InputTextBox.Text = "";
                InputTextBox.Focus();
                return;
            }

            if (Validate != null && !Validate.Invoke(InputTextBox.Text))
            {
                return;
            }

            this.Items.Add(InputTextBox.Text);

            // Clear the input, put the focus back on the text box.
            InputTextBox.Text = "";
            InputTextBox.Focus();
        }

        /// <summary>
        /// Handles key entry on the text box, mainly looking for the enter key.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TextKeyword_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Enter:
                    if (sender is TextBox tb)
                    {
                        // Suppress the enter key from bubbling up in case this is being
                        // used in an editor control that would close this dialog with the
                        // enter key.
                        e.Handled = true;
                        AddItem(tb.Text);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles key events for the list box, mainly the ability to delete items.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListItems_OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Delete:

                    if (sender is ListBox { SelectedItem: not null } listBox)
                    {
                        int currentIndex = listBox.SelectedIndex;
                        this.Items.RemoveAt(currentIndex);

                        // After removal, set the selected index appropriately
                        if (currentIndex < listBox.Items.Count) // There is a next item
                        {
                            listBox.SelectedIndex = currentIndex;
                        }
                        else if (listBox.Items.Count > 0) // Removed item was the last one, select the new last item
                        {
                            listBox.SelectedIndex = listBox.Items.Count - 1;
                        }

                        // Optionally, if you want to ensure the new selected item is in view:
                        listBox.ScrollIntoView(listBox.SelectedItem);
                    }

                    break;
            }
        }

        /// <summary>
        /// Adds the selected item into the list box.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (this.GetTemplateChild("TextKeyword") is TextBox tb)
            {
                AddItem(tb.Text);
            }
        }
    }
}