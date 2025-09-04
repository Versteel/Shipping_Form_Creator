﻿using System.Windows.Input;

namespace Shipping_Form_CreatorV1.Utilities
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        public RelayCommand() { }

        // Parameterless command (keeps your current usage working)
        public RelayCommand(Action execute, Func<bool>? canExecute = null)
            : this(_ => execute(),
                   canExecute is null ? null : (_ => canExecute()))
        { }

        // Parameterized command (for Remove, etc.)
        public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) => _execute(parameter);

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

}
