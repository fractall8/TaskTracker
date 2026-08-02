namespace Services.Abstractions.FaqChat;

public interface IFaqChatStore
{
    bool IsOpen { get; }

    bool IsSending { get; }

    string? ErrorMessage { get; }

    IReadOnlyList<FaqChatMessage> Messages { get; }

    event Action? StateChanged;

    void Open();

    void Close();

    void Toggle();

    Task AskAsync(string question, CancellationToken ct = default);

    void Reset();
}
