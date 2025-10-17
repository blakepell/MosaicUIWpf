/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class KitchenSinkExample
    {
        public class ViewModel : DependencyObject
        { 
            public ObservableCollection<string> Usernames { get; set;  } = new ObservableCollection<string>()
            {
                "bpell",
                "bguzik",
                "dbbarret",
                "john.doe",
                "b.pell",
                "rhien"
            };


            public string Username
            {
                get { return (string)GetValue(UsernameProperty); }
                set { SetValue(UsernameProperty, value); }
            }

            // Using a DependencyProperty as the backing store for Username.  This enables animation, styling, binding, etc...
            public static readonly DependencyProperty UsernameProperty =
                DependencyProperty.Register("Username", typeof(string), typeof(ViewModel), new PropertyMetadata(""));

        }

        public KitchenSinkExample()
        {
            InitializeComponent();

            DataContext = new ViewModel();


            //DataContext = new Person()
            //{
            //    Username = "john.doe",
            //    Name = "John Doe",
            //    LastActive = new DateTime(2026, 1, 1),
            //    Age = 42,
            //    Active = true,
            //    Color = Colors.Blue
            //};
        }
    }
}