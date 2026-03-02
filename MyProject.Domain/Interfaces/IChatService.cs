using MyProject.Domain.Entities;
using MyProject.Domain.ValueObjects;

namespace MyProject.Domain.Interfaces;

public interface IChatService
{
    bool HasActiveAdvisors();
    ChatSession CreateSession(ApplicantInfo applicantInfo);
    IReadOnlyCollection<ChatSession> GetPendingChats();
    IReadOnlyCollection<ChatSession> GetAllChats(string advisorId);
    ChatSession? GetChat(Guid sessionId);
    ChatSession? TakeChat(Guid sessionId, string advisorId);
    ChatSession? TransferChat(Guid sessionId, string sourceAdvisorId, string targetAdvisorId, string transferBy, string? reason = null);
    ChatSession? CloseChat(Guid sessionId, string closedBy, string? reason = null);
    void SetAdvisorActive(string advisorId, string advisorName, bool isActive);
    IReadOnlyCollection<AdvisorState> GetAdvisors();
    ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text);
}
