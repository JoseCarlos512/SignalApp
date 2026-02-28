using LandingBackend.Models;

namespace LandingBackend.Services;

public interface IChatBackendClient
{
    Task<CreateChatSessionResponse?> CreateSessionAsync(CreateChatSessionRequest request, CancellationToken cancellationToken);
    Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken);
    Task<ChatMessage?> SendMessageAsync(Guid sessionId, SendMessageRequest request, CancellationToken cancellationToken);
    Task<ChatSession?> CloseChatAsync(Guid sessionId, CloseChatRequest request, CancellationToken cancellationToken);
}
