using ChatBackend.Domain.Enums;
using ChatBackend.Domain.ValueObjects;

namespace ChatBackend.Domain.Entities;

public class ChatSession
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public ApplicantInfo Applicant { get; init; } = new("", "", "", "");
    public ChatStatus Status { get; set; } = ChatStatus.Pending;
    public string? AssignedAdvisorId { get; set; }
    public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;
    public List<ChatMessage> Messages { get; init; } = [];
}
