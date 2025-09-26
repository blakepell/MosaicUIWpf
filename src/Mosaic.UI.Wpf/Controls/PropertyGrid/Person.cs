using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mosaic.UI.Wpf.Controls
{
    public enum Gender
    {
        Male,
        Female,
        NonBinary
    }

    public partial class Person : ObservableObject
    {
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

        [property: Category("Giving")]
        [ObservableProperty]
        private double _lifetimeGiving = 0.00;

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
