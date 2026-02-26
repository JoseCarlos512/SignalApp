using System.Collections.Concurrent;
using ChatBackend.Models;

namespace ChatBackend.Services;

public class InMemoryChatService : IChatService
{
    private readonly ConcurrentDictionary<Guid, ChatSession> _sessions = new();
    private readonly ConcurrentDictionary<string, AdvisorState> _advisors = new();

    public bool HasActiveAdvisors() => _advisors.Values.Any(a => a.IsActive);

    public ChatSession CreateSession(CreateChatSessionRequest request)
    {
        var session = new ChatSession
        {
            Applicant = new ApplicantInfo(request.Name, request.Dni, request.Phone, request.Email)
        };

        session.Messages.Add(new ChatMessage
        {
            SessionId = session.Id,
            SenderType = "system",
            SenderId = "system",
            Text = "Sesión iniciada. Esperando asesor..."
        });

        _sessions[session.Id] = session;
        return session;
    }

    public IReadOnlyCollection<ChatSession> GetPendingChats() => _sessions.Values
        .Where(s => s.Status == ChatStatus.Pending)
        .OrderBy(s => s.CreatedAt)
        .ToList();

    public ChatSession? GetChat(Guid sessionId) => _sessions.TryGetValue(sessionId, out var chat) ? chat : null;

    public ChatSession? TakeChat(Guid sessionId, string advisorId)
    {
        if (!_sessions.TryGetValue(sessionId, out var chat) || chat.Status != ChatStatus.Pending)
        {
            return null;
        }

        chat.Status = ChatStatus.Assigned;
        chat.AssignedAdvisorId = advisorId;
        chat.Messages.Add(new ChatMessage
        {
            SessionId = chat.Id,
            SenderType = "system",
            SenderId = "system",
            Text = "Asesor conectado al chat."
        });

        return chat;
    }

    public void SetAdvisorActive(string advisorId, string advisorName, bool isActive)
    {
        var advisor = _advisors.GetOrAdd(advisorId, _ => new AdvisorState
        {
            AdvisorId = advisorId,
            Name = advisorName,
            IsActive = isActive
        });
        advisor.IsActive = isActive;
    }

    public IReadOnlyCollection<AdvisorState> GetAdvisors() => _advisors.Values.ToList();

    public ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text)
    {
        if (!_sessions.TryGetValue(sessionId, out var chat))
        {
            return null;
        }

        var message = new ChatMessage
        {
            SessionId = sessionId,
            SenderType = senderType,
            SenderId = senderId,
            Text = text
        };

        chat.Messages.Add(message);
        return message;
    }

    public void AddConnection(string advisorId, string advisorName, string connectionId)
    {
        var advisor = _advisors.GetOrAdd(advisorId, _ => new AdvisorState
        {
            AdvisorId = advisorId,
            Name = advisorName,
            IsActive = false
        });

        lock (advisor.ConnectionIds)
        {
            advisor.ConnectionIds.Add(connectionId);
        }
    }

    public void RemoveConnection(string advisorId, string connectionId)
    {
        if (!_advisors.TryGetValue(advisorId, out var advisor))
        {
            return;
        }

        lock (advisor.ConnectionIds)
        {
            advisor.ConnectionIds.Remove(connectionId);
        }
    }
}
