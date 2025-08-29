/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2025 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Mosaic.UI.Wpf.Controls;

namespace MosaicWpfDemo.Views
{
    public partial class ShellView : UserControl, ISideMenuRecipient
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ShellView), new PropertyMetadata("Untitled"));

        public string? Title
        {
            get => (string?)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty ImageSourceProperty = DependencyProperty.Register(nameof(ImageSource), typeof(ImageSource), typeof(ShellView), new PropertyMetadata(default(ImageSource)));

        public ImageSource ImageSource
        {
            get => (ImageSource)GetValue(ImageSourceProperty);
            set => SetValue(ImageSourceProperty, value);
        }

        public static readonly DependencyProperty XamlResourcePathProperty = DependencyProperty.Register(nameof(XamlResourcePath), typeof(string), typeof(ShellView), new PropertyMetadata(default(string)));

        public string XamlResourcePath
        {
            get => (string)GetValue(XamlResourcePathProperty);
            set => SetValue(XamlResourcePathProperty, value);
        }

        public static readonly DependencyProperty CSharpResourcePathProperty = DependencyProperty.Register(nameof(CSharpResourcePath), typeof(string), typeof(ShellView), new PropertyMetadata(default(string)));

        public string CSharpResourcePath
        {
            get => (string)GetValue(CSharpResourcePathProperty);
            set => SetValue(CSharpResourcePathProperty, value);
        }

        public static readonly DependencyProperty UserControlProperty = DependencyProperty.Register(nameof(UserControl), typeof(UserControl), typeof(ShellView), new PropertyMetadata(default(UserControl)));

        public UserControl UserControl
        {
            get => (UserControl)GetValue(UserControlProperty);
            set => SetValue(UserControlProperty, value);
        }

        public static readonly DependencyProperty DocumentationTypeProperty = DependencyProperty.Register(nameof(DocumentationType), typeof(Type), typeof(ShellView), new PropertyMetadata(default(Type)));

        public Type DocumentationType
        {
            get => (Type)GetValue(DocumentationTypeProperty);
            set => SetValue(DocumentationTypeProperty, value);
        }

        public IReadOnlyDictionary<string, object?> Parameters { get; set; }

        public void Refresh()
        {
            this.Title = this.Parameters["Title"]?.ToString() ?? "Untitled";
            this.XamlResourcePath = this.Parameters["XamlFile"].ToString();
            this.CSharpResourcePath = this.Parameters["CodeFile"].ToString();
            this.DocumentationType = this.Parameters["DocumentationType"] as Type;

            // TODO: Get Image Source
            //this.ImageSource = new ImageSourceConverter().ConvertFromString(this.Parameters["ImageSource"].ToString()) as ImageSource;

            var t = this.Parameters["ExampleType"] as Type;
            this.UserControl = Activator.CreateInstance(t) as UserControl;
        }

        public ShellView()
        {
            InitializeComponent();
            XamlEditor.Options.EnableHyperlinks = false;
            XamlEditor.Options.EnableEmailHyperlinks = false;
        }
    }
}
