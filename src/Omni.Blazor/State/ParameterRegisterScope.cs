namespace Omni.Blazor.State;

/// <summary>
/// Holds the set of <see cref="ParameterState{T}"/> objects registered by a
/// component, and runs their change-detection cycle after each
/// <c>SetParametersAsync</c>. One scope per component instance.
/// </summary>
public sealed class ParameterRegisterScope
{
    // Boxed via the non-generic interface so we can iterate without knowing T.
    private readonly List<IDetectable> _states = new();

    /// <summary>Start fluently registering a new parameter state.</summary>
    public ParameterStateBuilder<T> RegisterParameter<T>(string name) => new(this, name);

    internal void Add<T>(ParameterState<T> state) => _states.Add(new Wrapper<T>(state));

    /// <summary>Run change detection on every registered state, in registration order.</summary>
    internal async Task DetectAllAsync()
    {
        foreach (var s in _states) await s.DetectAsync();
    }

    private interface IDetectable
    {
        Task DetectAsync();
    }

    private sealed class Wrapper<T> : IDetectable
    {
        private readonly ParameterState<T> _state;
        public Wrapper(ParameterState<T> state) => _state = state;
        public Task DetectAsync() => _state.DetectAsync();
    }
}
