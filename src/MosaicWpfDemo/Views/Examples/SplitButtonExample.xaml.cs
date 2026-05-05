/*
 * Mosaic UI for WPF
 *
 * @project lead      : Blake Pell
 * @website           : https://www.blakepell.com
 * @website           : https://www.apexgate.net
 * @copyright         : Copyright (c), 2023-2026 All rights reserved.
 * @license           : MIT - https://opensource.org/license/mit/
 */

using System.Windows;
using System.Windows.Input;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class SplitButtonExample
    {
        public static readonly DependencyProperty StatusProperty = DependencyProperty.Register(
            nameof(Status), typeof(string), typeof(SplitButtonExample), new PropertyMetadata("No action yet."));

        public string Status
        {
            get => (string)GetValue(StatusProperty);
            set => SetValue(StatusProperty, value);
        }

        public ICommand PrimaryCommand { get; }

        public ICommand MenuCommand { get; }

        public ICommand DisabledPrimaryCommand { get; }

        public SplitButtonExample()
        {
            PrimaryCommand = new DelegateCommand(_ => Status = "Primary command executed.");
            MenuCommand = new DelegateCommand(parameter => Status = $"Menu command executed: {parameter}.");
            DisabledPrimaryCommand = new DelegateCommand(_ => Status = "This should not execute.", _ => false);

            DataContext = this;
            InitializeComponent();
        }

        private void SplitButton_OnClick(object sender, RoutedEventArgs e)
        {
            Status = "Primary click event raised.";
        }

        private void MenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Status = "Menu item click handler executed.";
        }

        private sealed class DelegateCommand : ICommand
        {
            private readonly Action<object?> _execute;
            private readonly Predicate<object?>? _canExecute;

            public DelegateCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
            {
                _execute = execute;
                _canExecute = canExecute;
            }

            public bool CanExecute(object? parameter)
            {
                return _canExecute?.Invoke(parameter) ?? true;
            }

            public void Execute(object? parameter)
            {
                _execute(parameter);
            }

            public event EventHandler? CanExecuteChanged
            {
                add { }
                remove { }
            }
        }
    }
}
