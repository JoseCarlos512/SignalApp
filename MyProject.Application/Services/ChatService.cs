using MyProject.Domain.Entities;
using MyProject.Domain.Interfaces;
using MyProject.Domain.ValueObjects;

namespace MyProject.Application.Services;

public class ChatService(IChatRepository chatRepository, IAdvisorRepository advisorRepository) : IChatService
{
    public bool HasActiveAdvisors() => advisorRepository.HasActiveAdvisors();

    public ChatSession CreateSession(ApplicantInfo applicantInfo)
    {
        return chatRepository.CreateSession(applicantInfo, "Sesión iniciada. Esperando asesor...");
    }

    public IReadOnlyCollection<ChatSession> GetPendingChats() => chatRepository.GetPendingChats();

    public IReadOnlyCollection<ChatSession> GetAllChats(string advisorId) => chatRepository.GetAllChats(advisorId);

    public ChatSession? GetChat(Guid sessionId) => chatRepository.GetChat(sessionId);

    public ChatSession? TakeChat(Guid sessionId, string advisorId)
    {
        return chatRepository.TakeChat(sessionId, advisorId, "Asesor conectado al chat.");
    }

    public ChatSession? TransferChat(Guid sessionId, string sourceAdvisorId, string targetAdvisorId, string transferBy, string? reason = null)
    {
        if (!advisorRepository.IsAdvisorActive(targetAdvisorId))
        {
            return null;
        }

        var message = string.IsNullOrWhiteSpace(reason)
            ? $"Solicitud derivada por {transferBy} al asesor {targetAdvisorId}."
            : $"Solicitud derivada por {transferBy} al asesor {targetAdvisorId}. Motivo: {reason}";

        return chatRepository.TransferChat(sessionId, sourceAdvisorId, targetAdvisorId, message);
    }

    public ChatSession? CloseChat(Guid sessionId, string closedBy, string? reason = null)
    {
        var message = string.IsNullOrWhiteSpace(reason)
            ? $"Chat cerrado por {closedBy}."
            : $"Chat cerrado por {closedBy}. Motivo: {reason}";

        return chatRepository.CloseChat(sessionId, closedBy, message);
    }

    public void SetAdvisorActive(string advisorId, string advisorName, bool isActive)
    {
        advisorRepository.SetAdvisorActive(advisorId, advisorName, isActive);
    }

    public IReadOnlyCollection<AdvisorState> GetAdvisors() => advisorRepository.GetAdvisors();

    public ChatMessage? AddMessage(Guid sessionId, string senderType, string senderId, string text)
    {
        return chatRepository.AddMessage(sessionId, senderType, senderId, text);
    }
}
