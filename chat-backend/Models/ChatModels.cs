namespace ChatBackend.Models;

public enum ChatStatus
{
    Pending,
    Assigned,
    Closed
}

public record ApplicantInfo(string Name, string Dni, string Phone, string Email);

public class ChatMessage
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Guid SessionId { get; init; }
    public string SenderType { get; init; } = "system"; // applicant | advisor | system
    public string SenderId { get; init; } = "system";
    public string Text { get; init; } = string.Empty;
    public DateTimeOffset SentAt { get; init; } = DateTimeOffset.UtcNow;
}

public class ChatSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ApplicantInfo Applicant { get; init; } = new("", "", "", "");
    public ChatStatus Status { get; set; } = ChatStatus.Pending;
    public string? AssignedAdvisorId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; init; } = new();
}

public class AdvisorState
{
    public string AdvisorId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public bool IsActive { get; set; }
    public HashSet<string> ConnectionIds { get; } = new();
}
