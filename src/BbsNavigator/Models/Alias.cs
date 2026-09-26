/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Cysharp.Text;

namespace BbsNavigator.Models
{
    /// <summary>
    /// An alias that runs a script when its name is entered as the first word in a terminal's
    /// command box.
    /// </summary>
    public partial class Alias : ObservableObject, ICloneable
    {
        public Alias()
        {
            Id = Guid.NewGuid().ToString();
        }

        /// <summary>
        /// The unique identifier for this alias.
        /// </summary>
        [property: Description("The unique identifier for the alias.  This is generally a GUID in string format.")]
        [property: ReadOnly(false)]
        [property: Browsable(false)]
        [ObservableProperty]
        public partial string Id { get; set; }

        /// <summary>
        /// The alias command to enter.
        /// </summary>
        [property: Display(Name = "Alias")]
        [property: Description("The name of the alias.  This is what will be typed into the input box to trigger the alias.")]
        [ObservableProperty]
        public partial string AliasExpression { get; set; } = "";

        /// <summary>
        /// The script to execute.
        /// </summary>
        [property: Display(Name = "Command")]
        [property: Description("The script that runs when this alias is triggered.  %1 through %9 are replaced with the words typed after the alias and %0 with all of them.")]
        [ObservableProperty]
        public partial string Command { get; set; } = "";

        /// <summary>
        /// The group that the alias belongs to.
        /// </summary>
        [property: Display(Name = "Group")]
        [property: Description("The name of the group this alias belongs to.  Groups can be used to enable / disable batches of functionality easily.")]
        [ObservableProperty]
        public partial string Group { get; set; } = "";

        /// <summary>
        /// The order the alias should be sorted in if rendered in other views like on a button repeater.
        /// </summary>
        [property: Display(Name = "Sort Order")]
        [property: Description("The order the alias should be sorted in if rendered in other views like on a button repeater.")]
        [ObservableProperty]
        public partial int SortOrder { get; set; } = 0;

        /// <summary>
        /// If the alias is currently enabled.
        /// </summary>
        [property: Display(Name = "Enabled")]
        [property: Description("If the alias is currently enabled.")]
        [property: Category("Options")]
        [property: ReadOnly(false)]
        [property: Browsable(true)]
        [ObservableProperty]
        public partial bool Enabled { get; set; } = true;

        ///// <summary>
        ///// The label to display on the button if it's rendered.
        ///// </summary>
        //[property: Display(Name = "Button Label")]
        //[property: Description("The label to display on the button if it's button is visible property is true.")]
        //[property: Category("UI")]
        //[property: ReadOnly(false)]
        //[property: Browsable(true)]
        //[ObservableProperty]
        //public partial string ButtonLabel { get; set; } = "";

        ///// <summary>
        ///// If the alias currently as a visible button on the alias actions repeater.
        ///// </summary>
        //[property: Display(Name = "Button Visible")]
        //[property: Category("UI")]
        //[property: Description("If a button for this alias should be visible on the alias actions panel.")]
        //[property: ReadOnly(false)]
        //[property: Browsable(true)]
        //[ObservableProperty]
        //public partial bool ButtonVisible { get; set; } = false;

        /// <summary>
        /// The number of times the alias has been used.
        /// </summary>
        [ObservableProperty]
        [property: Description("The number of times the alias has been executed.")]
        public partial int Count { get; set; } = 0;

        [property: Browsable(false)]
        [property: JsonIgnore]
        [ObservableProperty]
        public partial Visibility Visibility { get; set; } = Visibility.Visible;

        /// <summary>
        /// Gets the first line of the script, for display in lists.
        /// </summary>
        [JsonIgnore]
        public string CommandPreview
        {
            get
            {
                string command = Command.TrimStart();
                int end = command.IndexOfAny(['\r', '\n']);
                return end < 0 ? command : command[..end] + " …";
            }
        }

        partial void OnCommandChanged(string value)
        {
            OnPropertyChanged(nameof(CommandPreview));
        }

        public override string ToString()
        {
            using (var sb = ZString.CreateStringBuilder())
            {
                sb.Append(AliasExpression);
                sb.Append(' ');
                sb.Append(Command);
                sb.Append(' ');
                sb.Append(Group);

                return sb.ToString();
            }
        }

        /// <summary>
        /// Clones the alias, including its <see cref="Id"/>.
        /// </summary>
        public object Clone()
        {
            var clone = new Alias { Id = Id };
            clone.CopyFrom(this);
            return clone;
        }

        /// <summary>
        /// Copies every editable value from <paramref name="source"/>. The <see cref="Id"/> is not changed.
        /// </summary>
        /// <param name="source">The alias to copy from.</param>
        public void CopyFrom(Alias source)
        {
            ArgumentNullException.ThrowIfNull(source);
            AliasExpression = source.AliasExpression;
            Command = source.Command;
            Group = source.Group;
            SortOrder = source.SortOrder;
            Enabled = source.Enabled;
            Count = source.Count;
        }

        /// <summary>
        /// If the model is considered valid.
        /// </summary>
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(AliasExpression)
                   && !string.IsNullOrWhiteSpace(Command);
        }
    }
}
