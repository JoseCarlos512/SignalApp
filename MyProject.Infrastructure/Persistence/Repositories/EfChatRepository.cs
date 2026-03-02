using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Domain.Enums;
using MyProject.Domain.Interfaces;
using MyProject.Domain.ValueObjects;
using MyProject.Infrastructure.Persistence.Entities;

namespace MyProject.Infrastructure.Persistence.Repositories;

public class EfChatRepository(ChatDbContext dbContext) : IChatRepository
{
    public ChatSession CreateSession(ApplicantInfo applicantInfo, string initialMessage)
    {
        var session = new ChatSessionEntity
        {
            Name = applicantInfo.Name,
            Dni = applicantInfo.Dni,
            Phone = applicantInfo.Phone,
            Email = applicantInfo.Email
        };

        session.Messages.Add(new ChatMessageEntity
        {
            SessionId = session.Id,
            SenderType = "system",
            SenderId = "system",
            Text = initialMessage
        });

        dbContext.Sessions.Add(session);
        dbContext.SaveChanges();

        return ToDomain(session);
    }

    public IReadOnlyCollection<ChatSession> GetPendingChats() => dbContext.Sessions
        .AsNoTracking()
        .Where(s => s.Status == ChatStatus.Pending)
        .OrderBy(s => s.CreatedAt)
        .Include(s => s.Messages)
        .ToList()
        .Select(ToDomain)
        .ToList();

    public IReadOnlyCollection<ChatSession> GetAllChats(string advisorId) => dbContext.Sessions
        .AsNoTracking()
        .Where(s => s.Status == ChatStatus.Pending || s.AssignedAdvisorId == advisorId)
        .OrderByDescending(s => s.CreatedAt)
        .Include(s => s.Messages)
        .ToList()
        .Select(ToDomain)
        .ToList();

    public ChatSession? GetChat(Guid sessionId)
    {
        var chat = dbContext.Sessions.AsNoTracking().Include(s => s.Messages).FirstOrDefault(s => s.Id == sessionId);
        return chat is null ? null : ToDomain(chat);
    }

    public ChatSession? TakeChat(Guid sessionId, string advisorId, string systemMessage)
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

        dbContext.Messages.Add(new ChatMessageEntity
        {
            SessionId = sessionId,
            SenderType = "system",
            SenderId = "system",
            Text = systemMessage
        });
        dbContext.SaveChanges();

        return GetChat(sessionId);
    }

    public ChatSession? TransferChat(Guid sessionId, string sourceAdvisorId, string targetAdvisorId, string systemMessage)
    {
        var rowsAffected = dbContext.Sessions
            .Where(s => s.Id == sessionId && s.Status == ChatStatus.Assigned && s.AssignedAdvisorId == sourceAdvisorId)
            .ExecuteUpdate(s => s.SetProperty(p => p.AssignedAdvisorId, targetAdvisorId));

        if (rowsAffected == 0)
        {
            return null;
        }

        dbContext.Messages.Add(new ChatMessageEntity
        {
            SessionId = sessionId,
            SenderType = "system",
            SenderId = "system",
            Text = systemMessage
        });
        dbContext.SaveChanges();

        return GetChat(sessionId);
    }

    public ChatSession? CloseChat(Guid sessionId, string closedBy, string systemMessage)
    {
        var rowsAffected = dbContext.Sessions
            .Where(s => s.Id == sessionId && s.Status != ChatStatus.Closed)
            .ExecuteUpdate(s => s.SetProperty(p => p.Status, ChatStatus.Closed));

        if (rowsAffected == 0)
        {
            return null;
        }

        dbContext.Messages.Add(new ChatMessageEntity
        {
            SessionId = sessionId,
            SenderType = "system",
            SenderId = "system",
            Text = systemMessage
        });
        dbContext.SaveChanges();

        return GetChat(sessionId);
    }

    public ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text)
    {
        var chat = dbContext.Sessions.AsNoTracking().FirstOrDefault(s => s.Id == sessionId);
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

        return ToDomain(message);
    }

    private static ChatSession ToDomain(ChatSessionEntity entity) => new()
    {
        Id = entity.Id,
        Applicant = new ApplicantInfo(entity.Name, entity.Dni, entity.Phone, entity.Email),
        Status = entity.Status,
        AssignedAdvisorId = entity.AssignedAdvisorId,
        CreatedAt = entity.CreatedAt,
        Messages = entity.Messages.OrderBy(m => m.SentAt).Select(ToDomain).ToList()
    };

    private static ChatMessage ToDomain(ChatMessageEntity entity) => new()
    {
        Id = entity.Id,
        SessionId = entity.SessionId,
        SenderType = entity.SenderType,
        SenderId = entity.SenderId,
        Text = entity.Text,
        SentAt = entity.SentAt
    };
}
