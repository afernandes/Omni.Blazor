using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;

namespace Omni.Blazor.Tests.Ai;

/// <summary>An <see cref="IChatClient"/> that streams a fixed set of text chunks.</summary>
internal sealed class FakeChatClient : IChatClient
{
    private readonly string[] _chunks;

    public FakeChatClient(params string[] chunks) => _chunks = chunks;

    /// <summary>Messages captured from the most recent streaming call.</summary>
    public List<ChatMessage>? LastMessages { get; private set; }

    /// <summary>Options captured from the most recent streaming call.</summary>
    public ChatOptions? LastOptions { get; private set; }

    /// <summary>If set, the streaming call throws this before yielding anything.</summary>
    public Exception? ThrowOnStream { get; set; }

    public bool Disposed { get; private set; }

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToList();
        LastOptions = options;
        if (ThrowOnStream is not null) throw ThrowOnStream;
        foreach (string chunk in _chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);
            await Task.Yield();
        }
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, string.Concat(_chunks))));

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() => Disposed = true;
}

/// <summary>
/// An <see cref="IChatClient"/> that yields one chunk, then blocks on a gate until released
/// or cancelled — lets tests observe / interrupt a stream mid-flight deterministically.
/// </summary>
internal sealed class GatedChatClient : IChatClient
{
    private readonly TaskCompletionSource _gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _firstYielded = new(TaskCreationOptions.RunContinuationsAsynchronously);

    /// <summary>Completes once the first chunk has been yielded and the stream is parked on the gate.</summary>
    public Task FirstYielded => _firstYielded.Task;

    /// <summary>Releases the gate so the stream emits its final chunk.</summary>
    public void Release() => _gate.TrySetResult();

    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        yield return new ChatResponseUpdate(ChatRole.Assistant, "partial");
        _firstYielded.TrySetResult();
        await _gate.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        yield return new ChatResponseUpdate(ChatRole.Assistant, "-rest");
    }

    public Task<ChatResponse> GetResponseAsync(IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
        => Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "partial-rest")));

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }
}
