using System;
using System.Linq;
using UnityEditor.PackageManager;

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
    private readonly (Func<bool> fn, Action showMessage)[] _showResults;

    public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null, (Func<bool> fn, Action showMessage)[] getMessage = null)
    {
        if(execute == null)
            throw new ArgumentNullException(nameof(execute));
        
        _execute     = execute;
        _canExecute  = canExecute;
        _showResults = getMessage;
    }

    public bool CanExecute(object parameter = null)
        => _canExecute == null || _canExecute(parameter);

    public void Execute(object parameter = null)
    {
        if (CanExecute(parameter))
            _execute(parameter);
        else
            for(int i = 0; i < _showResults.Length; i++)
                if(_showResults[i].fn()) 
                    _showResults[i].showMessage();
    }
}