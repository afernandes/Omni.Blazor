namespace Omni.Blazor.State;

/// <summary>
/// Payload delivered to a <see cref="ParameterState{T}"/> change handler.
/// </summary>
public readonly record struct ParameterChangedEventArgs<T>(T LastValue, T Value);
