using Microsoft.AspNetCore.Components;

namespace Omni.Blazor.State;

/// <summary>
/// Fluent builder for a <see cref="ParameterState{T}"/>. Chain <c>WithParameter</c>,
/// <c>WithEventCallback</c>, <c>WithChangeHandler</c>, and optionally
/// <c>WithComparer</c>, then call <see cref="Attach"/> to register the state
/// with the owning component's scope.
/// </summary>
public sealed class ParameterStateBuilder<T>
{
    private readonly ParameterRegisterScope _scope;
    private readonly string _name;
    private Func<T>? _getter;
    private Func<EventCallback<T>>? _eventCallbackGetter;
    private Func<ParameterChangedEventArgs<T>, Task>? _changeHandler;
    private IEqualityComparer<T>? _comparer;

    internal ParameterStateBuilder(ParameterRegisterScope scope, string name)
    {
        _scope = scope;
        _name = name;
    }

    /// <summary>Bind the parameter getter, typically <c>() => Value</c>.</summary>
    public ParameterStateBuilder<T> WithParameter(Func<T> getter)
    {
        _getter = getter;
        return this;
    }

    /// <summary>Bind the matching <c>ValueChanged</c> callback (for two-way binding).</summary>
    public ParameterStateBuilder<T> WithEventCallback(Func<EventCallback<T>> getter)
    {
        _eventCallbackGetter = getter;
        return this;
    }

    /// <summary>Action invoked when the parameter changes (or on first detect).</summary>
    public ParameterStateBuilder<T> WithChangeHandler(Action handler)
    {
        _changeHandler = _ => { handler(); return Task.CompletedTask; };
        return this;
    }

    /// <summary>Action with args invoked when the parameter changes.</summary>
    public ParameterStateBuilder<T> WithChangeHandler(Action<ParameterChangedEventArgs<T>> handler)
    {
        _changeHandler = args => { handler(args); return Task.CompletedTask; };
        return this;
    }

    /// <summary>Async handler invoked when the parameter changes.</summary>
    public ParameterStateBuilder<T> WithChangeHandler(Func<ParameterChangedEventArgs<T>, Task> handler)
    {
        _changeHandler = handler;
        return this;
    }

    /// <summary>Override the default equality comparer.</summary>
    public ParameterStateBuilder<T> WithComparer(IEqualityComparer<T> comparer)
    {
        _comparer = comparer;
        return this;
    }

    /// <summary>Build the <see cref="ParameterState{T}"/> and register it with the scope.</summary>
    public ParameterState<T> Attach()
    {
        if (_getter is null)
            throw new InvalidOperationException(
                $"ParameterState for '{_name}' was attached without WithParameter(). " +
                "Always wire the getter before Attach().");

        var state = new ParameterState<T>(_name, _getter, _eventCallbackGetter, _changeHandler, _comparer);
        _scope.Add(state);
        return state;
    }
}
