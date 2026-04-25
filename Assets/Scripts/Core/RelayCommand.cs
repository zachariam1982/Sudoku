using System;

/// <summary>
/// A reusable ICommand that wraps an Action.
/// The ViewModel creates RelayCommands and exposes them to the View.
///
/// Usage:
///   SelectCellCommand = new RelayCommand(param => {
///       var (r, c) = ((int, int))param;
///       SelectCell(r, c);
///   });
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object>    _execute;
    private readonly Func<object, bool> _canExecute;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
    {
        if(execute == null)
            throw new ArgumentNullException(nameof(execute));
        
        _execute    = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object parameter = null)
        => _canExecute == null || _canExecute(parameter);

    public void Execute(object parameter = null)
    {
        if (CanExecute(parameter))
            _execute(parameter);
    }
}