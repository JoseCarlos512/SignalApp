namespace MyProject.Domain.Entities;

public class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public string SenderType { get; init; } = "system";
    public string SenderId { get; init; } = "system";
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}
