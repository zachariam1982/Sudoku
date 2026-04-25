using System;

/// <summary>
/// A generic observable property.
/// When Value is set, OnChanged is raised so bound Views can react.
/// 
/// Usage:
///   var score = new BindableProperty<int>(0);
///   score.OnChanged += newValue => scoreLabel.text = newValue.ToString();
///   score.Value = 10; // triggers OnChanged automatically
/// </summary>
public class BindableProperty<T>
{
    private T _value;

    /// <summary>Raised whenever Value changes.</summary>
    public event Action<T> OnChanged;

    public BindableProperty(T initialValue = default)
    {
        _value = initialValue;
    }

    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            OnChanged?.Invoke(_value);
        }
    }

    /// <summary>
    /// Forces OnChanged to fire even if the value hasn't changed.
    /// Useful when a mutable object (like an array) is modified in place.
    /// </summary>
    public void ForceNotify() => OnChanged?.Invoke(_value);
}