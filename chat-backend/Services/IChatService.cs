using ChatBackend.Models;

namespace ChatBackend.Services;

public interface IChatService
{
    bool HasActiveAdvisors();
    ChatSession CreateSession(CreateChatSessionRequest request);
    IReadOnlyCollection<ChatSession> GetPendingChats();
    ChatSession? GetChat(Guid sessionId);
    ChatSession? TakeChat(Guid sessionId, string advisorId);
    ChatSession? CloseChat(Guid sessionId, string closedBy, string? reason = null);
    void SetAdvisorActive(string advisorId, string advisorName, bool isActive);
    IReadOnlyCollection<AdvisorState> GetAdvisors();
    ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text);
    void AddConnection(string advisorId, string advisorName, string connectionId);
    void RemoveConnection(string advisorId, string connectionId);
}
