/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using CommunityToolkit.Mvvm.ComponentModel;
using Mosaic.UI.Wpf.Controls;
using System.Collections.ObjectModel;
using System.Windows;
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
                Color = Colors.Blue,
                Font = new FontFamily("Courier New"),
                Address = new Address
                {
                    Street = "742 Evergreen Terrace",
                    City = "Springfield",
                    State = "IL",
                    PostalCode = "62704",
                    IsPrimary = true
                }
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
        [property: PropertyGrid(Ignore = true)]
        [property: Category("Identity")]
        [ObservableProperty]
        private int _id;

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
        private DateOnly? _birthday;

        [property: Category("Account")]
        [ObservableProperty]
        private bool _active;

        [property: PropertyGrid(IsReadOnly = true, Description = "This field is read only.")]
        [property: Category("Account")]
        [ObservableProperty]
        private bool _disabledBool;

        [property: Category("Account")]
        [property: PropertyGrid(EditorType = typeof(FolderPropertyEditor), IsReadOnly = true)]
        [ObservableProperty]
        private string? _homeFolder;

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

        [property: Category("Appearance")]
        [property: Description("The font used to render the person's name.")]
        [ObservableProperty]
        private FontFamily? _font;

        [property: Category("Appearance")]
        [property: Description("The weight the person's name is rendered with.")]
        [ObservableProperty]
        private FontWeight _fontWeight = FontWeights.Normal;

        [property: Category("Appearance")]
        [property: Description("The style the person's name is rendered with.")]
        [ObservableProperty]
        private FontStyle _fontStyle = FontStyles.Normal;

        [property: Category("Identity")]
        [ObservableProperty]
        private Gender _gender;

        [property: Category("Identity")]
        [property: PropertyGrid(EditorType = typeof(FilePropertyEditor), IsReadOnly = true)]
        [ObservableProperty]
        private string? _profilePicturePath;

        [property: Category("Identity")]
        [ObservableProperty]
        private ObservableCollection<string> _nickNames = new();

        /// <summary>
        /// A sub object: the grid shows a chevron next to it that expands a nested, inset property grid.
        /// </summary>
        [property: Category("Contact")]
        [property: Description("The person's mailing address. Expand it to edit the individual fields.")]
        [ObservableProperty]
        private Address? _address;
    }

    public partial class Address : ObservableObject
    {
        [property: Description("The street address.")]
        [ObservableProperty]
        private string? _street;

        [property: Description("The city.")]
        [ObservableProperty]
        private string? _city;

        [property: PropertyGrid(MaxLength = 2)]
        [property: Description("The two letter state abbreviation.")]
        [ObservableProperty]
        private string? _state;

        [property: DisplayName("Postal Code")]
        [property: Description("The ZIP or postal code.")]
        [ObservableProperty]
        private string? _postalCode;

        [property: DisplayName("Primary Address")]
        [property: Description("Whether this is the person's primary address.")]
        [ObservableProperty]
        private bool _isPrimary;

        /// <summary>
        /// Shown as the summary in the value cell of the collapsed row.
        /// </summary>
        public override string ToString() => $"{Street}, {City}, {State} {PostalCode}";
    }
}