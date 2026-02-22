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
using System.Windows.Input;

namespace MosaicWpfDemo.Views.Examples
{
    public partial class ToggleSwitchExample
    {
        public static readonly DependencyProperty IsOnProperty = DependencyProperty.Register(
            nameof(IsOn), typeof(bool), typeof(ToggleSwitchExample), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty LastEventStateProperty = DependencyProperty.Register(
            nameof(LastEventState), typeof(string), typeof(ToggleSwitchExample), new PropertyMetadata("Off"));

        public static readonly DependencyProperty CommandInvocationsProperty = DependencyProperty.Register(
            nameof(CommandInvocations), typeof(int), typeof(ToggleSwitchExample), new PropertyMetadata(0));

        public bool IsOn
        {
            get => (bool)GetValue(IsOnProperty);
            set => SetValue(IsOnProperty, value);
        }

        public string LastEventState
        {
            get => (string)GetValue(LastEventStateProperty);
            set => SetValue(LastEventStateProperty, value);
        }

        public int CommandInvocations
        {
            get => (int)GetValue(CommandInvocationsProperty);
            set => SetValue(CommandInvocationsProperty, value);
        }

        public ICommand ToggleCommand { get; }

        public ToggleSwitchExample()
        {
            ToggleCommand = new DelegateCommand(_ => CommandInvocations++);

            DataContext = this;
            InitializeComponent();

            IsOn = true;
        }

        private void ToggleSwitch_OnToggled(object sender, RoutedPropertyChangedEventArgs<bool> e)
        {
            LastEventState = e.NewValue ? "On" : "Off";
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
