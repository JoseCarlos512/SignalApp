using ChatBackend.Domain.Entities;
using ChatBackend.Domain.ValueObjects;

namespace ChatBackend.Domain.Interfaces;

public interface IChatRepository
{
    ChatSession CreateSession(ApplicantInfo applicantInfo, string initialMessage);
    IReadOnlyCollection<ChatSession> GetPendingChats();
    IReadOnlyCollection<ChatSession> GetAllChats(string advisorId);
    ChatSession? GetChat(Guid sessionId);
    ChatSession? TakeChat(Guid sessionId, string advisorId, string systemMessage);
    ChatSession? TransferChat(Guid sessionId, string sourceAdvisorId, string targetAdvisorId, string systemMessage);
    ChatSession? CloseChat(Guid sessionId, string systemMessage);
    ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text);
}
