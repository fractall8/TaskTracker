using Contracts.Enums;
using Services.Abstractions.FaqChat;

namespace Services.FaqChat.Stores;

public class FaqChatStore(IFaqChatApiService faqChatApiService) : IFaqChatStore
{
    // Individual messages, not exchanges — 12 is six question-and-answer pairs. Must not exceed the
    // server's FaqChat:MaxHistoryTurns, which rejects anything longer.
    private const int _maxHistoryTurns = 12;

    private readonly List<FaqChatMessage> _messages = [];
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    public bool IsOpen { get; private set; }

    public bool IsSending { get; private set; }

    public string? ErrorMessage { get; private set; }

    public IReadOnlyList<FaqChatMessage> Messages => _messages;

    public event Action? StateChanged;

    public void Open()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;
        NotifyStateChanged();
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;
        NotifyStateChanged();
    }

    public void Toggle()
    {
        IsOpen = !IsOpen;
        NotifyStateChanged();
    }

    public async Task AskAsync(string question, CancellationToken ct = default)
    {
        var trimmed = question.Trim();

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return;
        }

        // Non-blocking guard: a double-submit is dropped rather than queued behind the in-flight call.
        if (!await _sendLock.WaitAsync(0, ct))
        {
            return;
        }

        try
        {
            IsSending = true;
            ErrorMessage = null;

            // Snapshot before appending, so the new question isn't duplicated into its own history.
            var history = _messages
                .TakeLast(_maxHistoryTurns)
                .Select(message => message.ToTurn())
                .ToList();

            _messages.Add(new FaqChatMessage(FaqChatRoleDto.User, trimmed));
            NotifyStateChanged();

            var answer = await faqChatApiService.AskAsync(trimmed, history, ct);

            _messages.Add(new FaqChatMessage(
                FaqChatRoleDto.Assistant,
                answer.Answer,
                answer.Kind,
                answer.Citations));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsSending = false;
            _sendLock.Release();
            NotifyStateChanged();
        }
    }

    public void Reset()
    {
        IsOpen = false;
        IsSending = false;
        ErrorMessage = null;
        _messages.Clear();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => StateChanged?.Invoke();
}
