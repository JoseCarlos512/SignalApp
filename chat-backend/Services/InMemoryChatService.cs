using System.Collections.Concurrent;
using ChatBackend.Data;
using ChatBackend.Data.Entities;
using ChatBackend.Models;
using Microsoft.EntityFrameworkCore;

namespace ChatBackend.Services;

public class InMemoryChatService(ChatDbContext dbContext) : IChatService
{
    private readonly ConcurrentDictionary<string, HashSet<string>> _advisorConnections = new();

    public bool HasActiveAdvisors() => dbContext.Advisors.Any(a => a.IsActive);

    public ChatSession CreateSession(CreateChatSessionRequest request)
    {
        var session = new ChatSessionEntity
        {
            Name = request.Name,
            Dni = request.Dni,
            Phone = request.Phone,
            Email = request.Email
        };

        session.Messages.Add(new ChatMessageEntity
        {
            SessionId = session.Id,
            SenderType = "system",
            SenderId = "system",
            Text = "Sesión iniciada. Esperando asesor..."
        });

        dbContext.Sessions.Add(session);
        dbContext.SaveChanges();

        return ToModel(session);
    }

    public IReadOnlyCollection<ChatSession> GetPendingChats() => dbContext.Sessions
        .AsNoTracking()
        .Where(s => s.Status == ChatStatus.Pending)
        .OrderBy(s => s.CreatedAt)
        .Include(s => s.Messages)
        .ToList()
        .Select(ToModel)
        .ToList();

    public IReadOnlyCollection<ChatSession> GetAllChats() => dbContext.Sessions
        .AsNoTracking()
        .OrderByDescending(s => s.CreatedAt)
        .Include(s => s.Messages)
        .ToList()
        .Select(ToModel)
        .ToList();

    public ChatSession? GetChat(Guid sessionId)
    {
        var chat = dbContext.Sessions
            .AsNoTracking()
            .Include(s => s.Messages)
            .FirstOrDefault(s => s.Id == sessionId);

        return chat is null ? null : ToModel(chat);
    }

    public ChatSession? TakeChat(Guid sessionId, string advisorId)
    {
        var rowsAffected = dbContext.Sessions
            .Where(s => s.Id == sessionId && s.Status == ChatStatus.Pending)
            .ExecuteUpdate(s => s
                .SetProperty(p => p.Status, ChatStatus.Assigned)
                .SetProperty(p => p.AssignedAdvisorId, advisorId));

        if (rowsAffected == 0)
        {
            return null;
        }

        var chat = dbContext.Sessions
            .Include(s => s.Messages)
            .FirstOrDefault(s => s.Id == sessionId);

        if (chat is null)
        {
            return null;
        }

        chat.Messages.Add(new ChatMessageEntity
        {
            SessionId = chat.Id,
            SenderType = "system",
            SenderId = "system",
            Text = "Asesor conectado al chat."
        });

        dbContext.SaveChanges();

        return ToModel(chat);
    }

    public ChatSession? TransferChat(Guid sessionId, string sourceAdvisorId, string targetAdvisorId, string transferBy, string? reason = null)
    {
        var targetAdvisorIsActive = dbContext.Advisors
            .Any(a => a.AdvisorId == targetAdvisorId && a.IsActive);

        if (!targetAdvisorIsActive)
        {
            return null;
        }

        var rowsAffected = dbContext.Sessions
            .Where(s => s.Id == sessionId && s.Status == ChatStatus.Assigned && s.AssignedAdvisorId == sourceAdvisorId)
            .ExecuteUpdate(s => s
                .SetProperty(p => p.AssignedAdvisorId, targetAdvisorId));

        if (rowsAffected == 0)
        {
            return null;
        }

        var chat = dbContext.Sessions
            .Include(s => s.Messages)
            .FirstOrDefault(s => s.Id == sessionId);

        if (chat is null)
        {
            return null;
        }

        chat.Messages.Add(new ChatMessageEntity
        {
            SessionId = chat.Id,
            SenderType = "system",
            SenderId = "system",
            Text = string.IsNullOrWhiteSpace(reason)
                ? $"Solicitud derivada por {transferBy} al asesor {targetAdvisorId}."
                : $"Solicitud derivada por {transferBy} al asesor {targetAdvisorId}. Motivo: {reason}"
        });

        dbContext.SaveChanges();

        return ToModel(chat);
    }

    public ChatSession? CloseChat(Guid sessionId, string closedBy, string? reason = null)
    {
        var rowsAffected = dbContext.Sessions
            .Where(s => s.Id == sessionId && s.Status != ChatStatus.Closed)
            .ExecuteUpdate(s => s
                .SetProperty(p => p.Status, ChatStatus.Closed));

        if (rowsAffected == 0)
        {
            return null;
        }

        var chat = dbContext.Sessions
            .Include(s => s.Messages)
            .FirstOrDefault(s => s.Id == sessionId);

        if (chat is null)
        {
            return null;
        }

        chat.Messages.Add(new ChatMessageEntity
        {
            SessionId = chat.Id,
            SenderType = "system",
            SenderId = "system",
            Text = string.IsNullOrWhiteSpace(reason)
                ? $"Chat cerrado por {closedBy}."
                : $"Chat cerrado por {closedBy}. Motivo: {reason}"
        });

        dbContext.SaveChanges();

        return ToModel(chat);
    }

    public void SetAdvisorActive(string advisorId, string advisorName, bool isActive)
    {
        var advisor = dbContext.Advisors.FirstOrDefault(a => a.AdvisorId == advisorId);
        if (advisor is null)
        {
            advisor = new AdvisorStateEntity
            {
                AdvisorId = advisorId,
                Name = advisorName,
                IsActive = isActive
            };
            dbContext.Advisors.Add(advisor);
        }
        else
        {
            advisor.Name = advisorName;
            advisor.IsActive = isActive;
        }

        dbContext.SaveChanges();
    }

    public IReadOnlyCollection<AdvisorState> GetAdvisors() => dbContext.Advisors
        .AsNoTracking()
        .Select(a => new AdvisorState
        {
            AdvisorId = a.AdvisorId,
            Name = a.Name,
            IsActive = a.IsActive
        })
        .ToList();

    public ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text)
    {
        var chat = dbContext.Sessions
            .AsNoTracking()
            .FirstOrDefault(s => s.Id == sessionId);

        if (chat is null || chat.Status == ChatStatus.Closed)
        {
            return null;
        }

        var message = new ChatMessageEntity
        {
            SessionId = sessionId,
            SenderType = senderType,
            SenderId = senderId,
            Text = text
        };

        dbContext.Messages.Add(message);
        dbContext.SaveChanges();

        return ToModel(message);
    }

    public void AddConnection(string advisorId, string advisorName, string connectionId)
    {
        var connections = _advisorConnections.GetOrAdd(advisorId, _ => new HashSet<string>());

        lock (connections)
        {
            connections.Add(connectionId);
        }

        var advisor = dbContext.Advisors.FirstOrDefault(a => a.AdvisorId == advisorId);
        if (advisor is null)
        {
            dbContext.Advisors.Add(new AdvisorStateEntity
            {
                AdvisorId = advisorId,
                Name = advisorName,
                IsActive = false
            });
            dbContext.SaveChanges();
            return;
        }

        if (advisor.Name != advisorName)
        {
            advisor.Name = advisorName;
            dbContext.SaveChanges();
        }
    }

    public void RemoveConnection(string advisorId, string connectionId)
    {
        if (!_advisorConnections.TryGetValue(advisorId, out var connections))
        {
            return;
        }

        lock (connections)
        {
            connections.Remove(connectionId);
            if (connections.Count == 0)
            {
                _advisorConnections.TryRemove(advisorId, out _);
            }
        }
    }

    private static ChatSession ToModel(ChatSessionEntity entity) => new()
    {
        Id = entity.Id,
        Applicant = new ApplicantInfo(entity.Name, entity.Dni, entity.Phone, entity.Email),
        Status = entity.Status,
        AssignedAdvisorId = entity.AssignedAdvisorId,
        CreatedAt = entity.CreatedAt,
        Messages = entity.Messages
            .OrderBy(m => m.SentAt)
            .Select(ToModel)
            .ToList()
    };

    private static ChatMessage ToModel(ChatMessageEntity entity) => new()
    {
        Id = entity.Id,
        SessionId = entity.SessionId,
        SenderType = entity.SenderType,
        SenderId = entity.SenderId,
        Text = entity.Text,
        SentAt = entity.SentAt
    };
}
