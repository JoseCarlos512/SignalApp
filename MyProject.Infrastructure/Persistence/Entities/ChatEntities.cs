using MyProject.Domain.Enums;

namespace MyProject.Infrastructure.Persistence.Entities;

public class ChatSessionEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public ChatStatus Status { get; set; } = ChatStatus.Pending;
    public string? AssignedAdvisorId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<ChatMessageEntity> Messages { get; set; } = new();
}

public class ChatMessageEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SessionId { get; set; }
    public ChatSessionEntity Session { get; set; } = null!;
    public string SenderType { get; set; } = "system";
    public string SenderId { get; set; } = "system";
    public string Text { get; set; } = string.Empty;
    public DateTimeOffset SentAt { get; set; } = DateTimeOffset.UtcNow;
}

public class AdvisorStateEntity
{
    public string AdvisorId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}
