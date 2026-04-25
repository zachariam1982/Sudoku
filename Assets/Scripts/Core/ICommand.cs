/// <summary>
/// Command interface for the MVVM pattern.
/// Views call Execute() — the ViewModel decides what happens.
/// </summary>
public interface ICommand
{
    void Execute(object parameter = null);
    bool CanExecute(object parameter = null);
}