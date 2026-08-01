using System.Diagnostics;
using Microsoft.Extensions.AI;
using Omni.Blazor.Components;
using Omni.Blazor.Models;

namespace Omni.Blazor.Ai;

/// <summary>
/// Headless AI conversation orchestrator — the "brain" the Omni AI primitives plug into.
/// Wraps any <see cref="IChatClient"/> (the standard Microsoft.Extensions.AI seam, so you
/// keep telemetry / function-invocation / caching middleware and zero provider lock-in),
/// owns the turn list + system prompt + sliding-window history, drives the streaming
/// <c>await foreach</c>, and raises <see cref="Changed"/> so a component can re-render.
///
/// <para>
/// It is UI-agnostic: <see cref="OmniAiConversation"/> binds to it for a drop-in chat, but
/// you can also drive it from your own markup using the <c>Components/Ai</c> primitives.
/// </para>
/// </summary>
public sealed class OmniChatClient : IAsyncDisposable, IDisposable
{
    private readonly IChatClient _client;
    private readonly List<OmniChatTurn> _turns = [];
    private readonly object _stateSync = new();
    private readonly SemaphoreSlim _sendGate = new(1, 1);
    private OmniChatTurn[] _turnSnapshot = [];
    private CancellationTokenSource? _cts;
    private int _isStreaming;
    private int _disposeState;
    private int _disposeClientPending;
    private int _ownedClientDisposeState;

    // Render coalescing: during streaming, tokens can arrive faster than the UI can
    // usefully repaint. Raising Changed per token forces a full Markdown reparse of the
    // growing text every time (O(n²)); instead we coalesce to ~30fps. The final Raise()
    // in SendAsync's finally guarantees the last tokens always render.
    private static readonly long RenderThrottleTicks = Stopwatch.Frequency / 30;
    private long _lastRaiseTicks;

    // Monotonic clock, overridable in tests to make the throttle deterministic.
    internal Func<long> NowTicks { get; set; } = Stopwatch.GetTimestamp;

    private readonly bool _disposeClient;

    /// <summary>
    /// Creates a conversation orchestrator over a client owned by the caller or DI container.
    /// </summary>
    /// <param name="client">The chat client to talk to (any provider via Microsoft.Extensions.AI).</param>
    /// <param name="options">Conversation options (system prompt, history cap, inference options).</param>
    public OmniChatClient(IChatClient client, OmniChatOptions? options = null)
        : this(client, options, disposeClient: false)
    {
    }

    /// <summary>
    /// Creates a conversation orchestrator with default options and explicit client ownership.
    /// </summary>
    public OmniChatClient(IChatClient client, bool disposeClient)
        : this(client, options: null, disposeClient)
    {
    }

    /// <summary>
    /// Creates a conversation orchestrator with explicit ownership of the underlying client.
    /// </summary>
    /// <param name="client">The chat client to talk to.</param>
    /// <param name="options">Conversation options.</param>
    /// <param name="disposeClient">
    /// Whether disposing this <see cref="OmniChatClient"/> should also dispose
    /// <paramref name="client"/>. Keep this <c>false</c> for shared or DI-managed clients.
    /// </param>
    public OmniChatClient(IChatClient client, OmniChatOptions? options, bool disposeClient)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Options = options ?? new OmniChatOptions();
        _disposeClient = disposeClient;
    }

    /// <summary>Conversation options. Mutable — change the system prompt or model between turns.</summary>
    public OmniChatOptions Options { get; set; }

    /// <summary>The conversation so far (user / assistant / system turns), in order.</summary>
    public IReadOnlyList<OmniChatTurn> Turns => Volatile.Read(ref _turnSnapshot);

    /// <summary>True while a response is streaming in.</summary>
    public bool IsStreaming => Volatile.Read(ref _isStreaming) != 0;

    /// <summary>Raised whenever the conversation changes (new turn, streamed token, cleared).</summary>
    public event Action? Changed;

    /// <summary>
    /// Send a user message and stream the assistant's reply into a new turn. No-ops on blank
    /// input or while a response is already streaming (one turn at a time — it does not queue
    /// or interrupt). Cancelling <paramref name="cancellationToken"/> aborts the in-flight stream,
    /// keeping whatever streamed so far.
    /// </summary>
    public async Task SendAsync(string userText, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        if (string.IsNullOrWhiteSpace(userText) || IsStreaming) return;

        // The public contract says a concurrent send is ignored, not queued.
        // WaitAsync(0) closes the check/set race without creating a convoy.
        if (!await _sendGate.WaitAsync(0, cancellationToken).ConfigureAwait(false)) return;

        CancellationTokenSource? cts = null;
        OmniChatTurn? assistant = null;
        try
        {
            ThrowIfDisposed();
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken token = cts.Token;

            lock (_stateSync)
            {
                ThrowIfDisposed();
                _cts = cts;
                _turns.Add(new OmniChatTurn(MessageRole.User, userText.Trim()));
                assistant = new OmniChatTurn(MessageRole.Assistant) { IsStreaming = true };
                _turns.Add(assistant);
                PublishTurnSnapshot();
                Volatile.Write(ref _isStreaming, 1);
            }

            Raise();
            _lastRaiseTicks = NowTicks() - RenderThrottleTicks - 1;

            await foreach (ChatResponseUpdate update in _client.GetStreamingResponseAsync(BuildRequest(), Options.ChatOptions, token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) break;
                string text = update.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    assistant.Content += text;
                    long now = NowTicks();
                    if (now - _lastRaiseTicks >= RenderThrottleTicks)
                    {
                        _lastRaiseTicks = now;
                        Raise();
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cts?.IsCancellationRequested is true)
        {
            // Disposal or caller cancellation keeps the partial response.
        }
        catch (Exception ex) when (assistant is not null)
        {
            assistant.IsError = true;
            if (string.IsNullOrEmpty(assistant.Content))
                assistant.Content = Options.ErrorMessage ?? $"⚠️ {ex.Message}";
        }
        finally
        {
            if (assistant is not null) assistant.IsStreaming = false;
            Volatile.Write(ref _isStreaming, 0);

            lock (_stateSync)
            {
                if (ReferenceEquals(_cts, cts)) _cts = null;
            }

            // The send owns its linked source. Dispose unregisters it from a
            // potentially long-lived caller token without racing active use.
            cts?.Dispose();
            if (!IsDisposed) Raise();
            _sendGate.Release();

            if (IsDisposed && Volatile.Read(ref _disposeClientPending) != 0)
                DisposeOwnedClientAfterSend();
        }
    }

    /// <summary>Append a turn without calling the model (e.g. seed history or a greeting).</summary>
    public void AddTurn(OmniChatTurn turn)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(turn);
        lock (_stateSync)
        {
            _turns.Add(turn);
            PublishTurnSnapshot();
        }
        Raise();
    }

    /// <summary>Clear the whole conversation.</summary>
    public void Clear()
    {
        ThrowIfDisposed();
        lock (_stateSync)
        {
            _turns.Clear();
            PublishTurnSnapshot();
        }
        Raise();
    }

    // The message list sent to the model: system prompt + the (windowed) completed turns.
    private IReadOnlyList<ChatMessage> BuildRequest()
    {
        lock (_stateSync)
        {
            var messages = new List<ChatMessage>(_turns.Count + 1);
            if (!string.IsNullOrEmpty(Options.SystemPrompt))
                messages.Add(new ChatMessage(ChatRole.System, Options.SystemPrompt));

            int start = 0;
            if (Options.MaxHistory is int max && max > 0)
            {
                int completed = 0;
                for (int index = _turns.Count - 1; index >= 0; index--)
                {
                    if (_turns[index].IsStreaming) continue;
                    completed++;
                    if (completed == max)
                    {
                        start = index;
                        break;
                    }
                }
            }

            for (int index = start; index < _turns.Count; index++)
            {
                OmniChatTurn turn = _turns[index];
                if (turn.IsStreaming) continue;
                messages.Add(new ChatMessage(ToRole(turn.Role), turn.Content));
            }
            return messages;
        }
    }

    private static ChatRole ToRole(MessageRole role) => role switch
    {
        MessageRole.User => ChatRole.User,
        MessageRole.System => ChatRole.System,
        _ => ChatRole.Assistant,
    };

    private void Raise() => Changed?.Invoke();

    private bool IsDisposed => Volatile.Read(ref _disposeState) != 0;

    private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(IsDisposed, this);

    private void PublishTurnSnapshot()
        => Volatile.Write(ref _turnSnapshot, [.. _turns]);

    /// <inheritdoc />
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) != 0) return;
        if (_disposeClient) Volatile.Write(ref _disposeClientPending, 1);

        CancellationTokenSource? active;
        lock (_stateSync)
        {
            active = _cts;
            _cts = null;
            Changed = null;
        }
        CancelSafely(active);

        // Only dispose the client when this instance exclusively owns it — a shared /
        // DI-managed IChatClient must outlive the conversation. When a send is active,
        // its finally block performs disposal after releasing the send gate.
        if (_disposeClient && active is null) DisposeOwnedClient();
        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposeState, 1) == 0)
        {
            CancellationTokenSource? active;
            lock (_stateSync)
            {
                active = _cts;
                _cts = null;
                Changed = null;
            }
            CancelSafely(active);
        }

        if (_disposeClient)
        {
            await _sendGate.WaitAsync().ConfigureAwait(false);
            try
            {
                await DisposeOwnedClientAsync().ConfigureAwait(false);
            }
            finally
            {
                _sendGate.Release();
            }
        }
        GC.SuppressFinalize(this);
    }

    private void DisposeOwnedClient()
    {
        if (!_disposeClient
            || Interlocked.Exchange(ref _ownedClientDisposeState, 1) != 0)
        {
            return;
        }

        _client.Dispose();
    }

    private void DisposeOwnedClientAfterSend()
    {
        try
        {
            DisposeOwnedClient();
        }
        catch (Exception exception)
        {
            // Synchronous Dispose already returned, so there is no caller to receive
            // a deferred cleanup exception. Observe it without faulting the send task.
            Debug.WriteLine(exception);
        }
    }

    private async ValueTask DisposeOwnedClientAsync()
    {
        if (!_disposeClient
            || Interlocked.Exchange(ref _ownedClientDisposeState, 1) != 0)
        {
            return;
        }

        if (_client is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _client.Dispose();
    }

    private static void CancelSafely(CancellationTokenSource? source)
    {
        if (source is null) return;

        try
        {
            source.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // The active send owns disposal and may have completed concurrently.
        }
    }
}
