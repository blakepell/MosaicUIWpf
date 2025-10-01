/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Windows.Media;
using System.ComponentModel;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class PropertyGridExample
    {
        public PropertyGridExample()
        {
            InitializeComponent();

            DataContext = new Person()
            {
                Username = "john.doe",
                Name = "John Doe",
                LastActive = new DateTime(2026, 1, 1),
                Age = 42,
                Active = true,
                Color = Colors.Blue
            };
        }
    }

    public enum Gender
    {
        Male,
        Female,
        Unspecified
    }

    public partial class Person : ObservableObject
    {
        [property: PropertyGrid(MaxLength = 12)]
        [property: DisplayName("Username")]
        [property: Category("Account")]
        [property: Description("The users account name.")]
        [ObservableProperty]
        private string? _username;

        [property: DisplayName("Full Name")]
        [property: Category("Identity")]
        [property: Description("The person's full name")]
        [ObservableProperty]
        private string _name;

        [property: Category("Identity")]
        [property: Description("The person's age")]
        [ObservableProperty]
        private int _age;

        [property: Category("Identity")]
        [ObservableProperty]
        private DateOnly _birthday;

        [property: Category("Account")]
        [ObservableProperty]
        private bool _active;

        [property: PropertyGrid(IsReadOnly = true, Description = "This field is read only.")]
        [property: Category("Account")]
        [ObservableProperty]
        private bool _disabledBool;

        [property: Category("Giving")]
        [ObservableProperty]
        private double _lifetimeGiving = 0.00;

        [property: PropertyGrid(IsReadOnly = true)]
        [ObservableProperty]
        private string _notes = "This is a readonly field.";

        [property: Category("Account")]
        [ObservableProperty]
        private DateTime? _lastActive;

        [property: Category("Appearance")]
        [ObservableProperty]
        private Color _color;

        [property: Category("Identity")]
        [ObservableProperty]
        private Gender _gender;

        [property: PropertyGrid(Ignore = true)]
        [property: Category("Identity")]
        [ObservableProperty]
        private ObservableCollection<string> _nickNames = new();
    }
}