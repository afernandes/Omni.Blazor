using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace Omni.Blazor.Services;

/// <summary>Evaluates named authorization policies once per DataGridForm schema refresh.</summary>
public interface IDataGridFormPolicyEvaluator
{
    /// <summary>Returns whether the current principal satisfies the named policy.</summary>
    ValueTask<bool> AuthorizeAsync(string policy, CancellationToken cancellationToken = default);
}

/// <summary>Delegate-backed policy evaluator useful for WASM clients, tests and custom permission stores.</summary>
public sealed class DelegateDataGridFormPolicyEvaluator(
    Func<string, CancellationToken, ValueTask<bool>> evaluator) : IDataGridFormPolicyEvaluator
{
    /// <inheritdoc />
    public ValueTask<bool> AuthorizeAsync(string policy, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        return evaluator(policy, cancellationToken);
    }
}

internal sealed class AspNetCoreDataGridFormPolicyEvaluator(IServiceProvider services)
    : IDataGridFormPolicyEvaluator
{
    public async ValueTask<bool> AuthorizeAsync(
        string policy,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policy);
        cancellationToken.ThrowIfCancellationRequested();
        IAuthorizationService? authorization = services.GetService<IAuthorizationService>();
        AuthenticationStateProvider? authenticationState = services.GetService<AuthenticationStateProvider>();
        if (authorization is null || authenticationState is null) return false;

        AuthenticationState state = await authenticationState.GetAuthenticationStateAsync();
        cancellationToken.ThrowIfCancellationRequested();
        AuthorizationResult result = await authorization.AuthorizeAsync(state.User, resource: null, policy);
        cancellationToken.ThrowIfCancellationRequested();
        return result.Succeeded;
    }
}
