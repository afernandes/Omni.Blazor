using Microsoft.Extensions.AI;
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
    private CancellationTokenSource? _cts;
    private bool _disposed;

    /// <param name="client">The chat client to talk to (any provider via Microsoft.Extensions.AI).</param>
    /// <param name="options">Conversation options (system prompt, history cap, inference options).</param>
    public OmniChatClient(IChatClient client, OmniChatOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        Options = options ?? new OmniChatOptions();
    }

    /// <summary>Conversation options. Mutable — change the system prompt or model between turns.</summary>
    public OmniChatOptions Options { get; set; }

    /// <summary>The conversation so far (user / assistant / system turns), in order.</summary>
    public IReadOnlyList<OmniChatTurn> Turns => _turns;

    /// <summary>True while a response is streaming in.</summary>
    public bool IsStreaming { get; private set; }

    /// <summary>Raised whenever the conversation changes (new turn, streamed token, cleared).</summary>
    public event Action? Changed;

    /// <summary>
    /// Send a user message and stream the assistant's reply into a new turn. No-ops on blank
    /// input or while a response is already streaming. Cancelling <paramref name="cancellationToken"/>
    /// (or sending again) aborts the in-flight stream.
    /// </summary>
    public async Task SendAsync(string userText, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (string.IsNullOrWhiteSpace(userText) || IsStreaming) return;

        _cts?.Cancel();
        _cts?.Dispose();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _cts = cts;
        CancellationToken token = cts.Token;

        AddTurn(new OmniChatTurn(MessageRole.User, userText.Trim()));
        var assistant = new OmniChatTurn(MessageRole.Assistant) { IsStreaming = true };
        AddTurn(assistant);
        IsStreaming = true;
        Raise();

        try
        {
            await foreach (ChatResponseUpdate update in _client.GetStreamingResponseAsync(BuildRequest(), Options.ChatOptions, token).ConfigureAwait(false))
            {
                if (token.IsCancellationRequested) break;
                string text = update.Text;
                if (!string.IsNullOrEmpty(text))
                {
                    assistant.Content += text;
                    Raise();
                }
            }
        }
        catch (OperationCanceledException)
        {
            // aborted by a new send or the caller — leave whatever streamed so far
        }
        catch (Exception ex)
        {
            assistant.IsError = true;
            if (string.IsNullOrEmpty(assistant.Content))
                assistant.Content = Options.ErrorMessage ?? $"⚠️ {ex.Message}";
        }
        finally
        {
            assistant.IsStreaming = false;
            IsStreaming = false;
            Raise();

            // Dispose the linked source so it deregisters from the caller's token
            // (otherwise a long-lived caller token would root this client — a leak).
            cts.Dispose();
            if (_cts == cts) _cts = null;
        }
    }

    /// <summary>Append a turn without calling the model (e.g. seed history or a greeting).</summary>
    public void AddTurn(OmniChatTurn turn)
    {
        ArgumentNullException.ThrowIfNull(turn);
        _turns.Add(turn);
        Raise();
    }

    /// <summary>Clear the whole conversation.</summary>
    public void Clear()
    {
        _turns.Clear();
        Raise();
    }

    // The message list sent to the model: system prompt + the (windowed) completed turns.
    private IEnumerable<ChatMessage> BuildRequest()
    {
        var messages = new List<ChatMessage>();
        if (!string.IsNullOrEmpty(Options.SystemPrompt))
            messages.Add(new ChatMessage(ChatRole.System, Options.SystemPrompt));

        IEnumerable<OmniChatTurn> history = Options.MaxHistory is int max && max > 0
            ? _turns.Where(t => !t.IsStreaming).TakeLast(max)
            : _turns;

        foreach (OmniChatTurn turn in history)
        {
            if (turn.IsStreaming) continue; // skip the empty assistant turn we're filling
            messages.Add(new ChatMessage(ToRole(turn.Role), turn.Content));
        }
        return messages;
    }

    private static ChatRole ToRole(MessageRole role) => role switch
    {
        MessageRole.User => ChatRole.User,
        MessageRole.System => ChatRole.System,
        _ => ChatRole.Assistant,
    };

    private void Raise() => Changed?.Invoke();

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        _cts?.Dispose();
        _client.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;
        try { _cts?.Cancel(); } catch (ObjectDisposedException) { }
        _cts?.Dispose();
        if (_client is IAsyncDisposable asyncDisposable)
            await asyncDisposable.DisposeAsync().ConfigureAwait(false);
        else
            _client.Dispose();
    }
}
