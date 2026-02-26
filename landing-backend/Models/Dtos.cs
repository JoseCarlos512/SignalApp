namespace LandingBackend.Models;

public record CreateChatSessionRequest(string Name, string Dni, string Phone, string Email);
public record CreateChatSessionResponse(Guid SessionId, string StatusMessage, bool AdvisorsAvailable);
public record SendMessageRequest(string SenderType, string SenderId, string Text);

public class ChatMessage
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public string SenderType { get; init; } = string.Empty;
    public string SenderId { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset SentAt { get; init; }
}

public class ChatSession
{
    public Guid Id { get; init; }
    public object Applicant { get; init; } = new();
    public string Status { get; init; } = string.Empty;
    public string? AssignedAdvisorId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<ChatMessage> Messages { get; init; } = [];
}
