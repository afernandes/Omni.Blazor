using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.State;

/// <summary>
/// Tracks a single Razor parameter and fires a handler when the value changes
/// across a render. Eliminates the manual <c>_lastValueParam</c> bookkeeping
/// that was causing footgun bugs in our reactive components.
///
/// You normally don't instantiate this directly — go through
/// <see cref="ParameterRegisterScope.RegisterParameter{T}"/> on
/// <c>OmniComponent</c>.
///
/// See <see cref="ParameterStateBuilder{T}"/> for the fluent API.
/// </summary>
public sealed class ParameterState<T>
{
    private readonly Func<T> _getter;
    private readonly Func<EventCallback<T>>? _eventCallbackGetter;
    private readonly Func<ParameterChangedEventArgs<T>, Task>? _changeHandler;
    private readonly IEqualityComparer<T> _comparer;

    private T _lastValue = default!;
    private bool _initialized;

    internal ParameterState(
        string name,
        Func<T> getter,
        Func<EventCallback<T>>? eventCallbackGetter,
        Func<ParameterChangedEventArgs<T>, Task>? changeHandler,
        IEqualityComparer<T>? comparer)
    {
        Name = name;
        _getter = getter;
        _eventCallbackGetter = eventCallbackGetter;
        _changeHandler = changeHandler;
        _comparer = comparer ?? EqualityComparer<T>.Default;
    }

    /// <summary>The parameter's <c>nameof(...)</c> name.</summary>
    public string Name { get; }

    /// <summary>Current value (re-reads the getter every call).</summary>
    public T Value => _getter();

    /// <summary>Value seen at the previous detect cycle.</summary>
    public T LastValue => _lastValue;

    /// <summary>True after the first detect cycle has run.</summary>
    public bool IsInitialized => _initialized;

    /// <summary>
    /// Convenience to invoke the bound <see cref="EventCallback{T}"/> (typically
    /// "ValueChanged"). Use when you mutate the bound state internally and need
    /// to push it back to the consumer.
    /// </summary>
    public Task SetValueAsync(T newValue)
    {
        if (_eventCallbackGetter is null) return Task.CompletedTask;
        var cb = _eventCallbackGetter();
        return cb.HasDelegate ? cb.InvokeAsync(newValue) : Task.CompletedTask;
    }

    /// <summary>
    /// Compare current vs last; invoke the change handler when different (or
    /// on the very first call). Called by <c>OmniComponent.SetParametersAsync</c>
    /// after each base render.
    /// </summary>
    internal async Task DetectAsync()
    {
        var current = _getter();
        if (_initialized && _comparer.Equals(current, _lastValue))
            return;

        var args = new ParameterChangedEventArgs<T>(_initialized ? _lastValue : current, current);
        _lastValue = current;
        _initialized = true;

        if (_changeHandler is not null)
            await _changeHandler(args);
    }
}
