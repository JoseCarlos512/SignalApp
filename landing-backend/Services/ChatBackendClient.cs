using System.Net;
using System.Net.Http.Json;
using LandingBackend.Models;

namespace LandingBackend.Services;

public class ChatBackendClient(HttpClient httpClient, IConfiguration configuration) : IChatBackendClient
{
    private readonly string _baseUrl = configuration["ChatBackend:BaseUrl"] ?? "http://localhost:5000";

    public async Task<CreateChatSessionResponse?> CreateSessionAsync(CreateChatSessionRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/api/chats/session", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CreateChatSessionResponse>(cancellationToken: cancellationToken);
    }

    public async Task<ChatSession?> GetSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var response = await httpClient.GetAsync($"{_baseUrl}/api/chats/{sessionId}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatSession>(cancellationToken: cancellationToken);
    }

    public async Task<ChatMessage?> SendMessageAsync(Guid sessionId, SendMessageRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/api/chats/{sessionId}/messages", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatMessage>(cancellationToken: cancellationToken);
    }

    public async Task<ChatSession?> CloseChatAsync(Guid sessionId, CloseChatRequest request, CancellationToken cancellationToken)
    {
        var response = await httpClient.PostAsJsonAsync($"{_baseUrl}/api/chats/{sessionId}/close", request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ChatSession>(cancellationToken: cancellationToken);
    }
}
