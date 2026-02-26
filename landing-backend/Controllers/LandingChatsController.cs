using LandingBackend.Models;
using LandingBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace LandingBackend.Controllers;

[ApiController]
[Route("api/chats")]
public class LandingChatsController(IChatBackendClient chatBackendClient) : ControllerBase
{
    [HttpPost("session")]
    public async Task<ActionResult<CreateChatSessionResponse>> CreateSession([FromBody] CreateChatSessionRequest request, CancellationToken cancellationToken)
    {
        var session = await chatBackendClient.CreateSessionAsync(request, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<ActionResult<ChatSession>> GetSession(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = await chatBackendClient.GetSessionAsync(sessionId, cancellationToken);
        return session is null ? NotFound() : Ok(session);
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<ActionResult<ChatMessage>> SendMessage(Guid sessionId, [FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await chatBackendClient.SendMessageAsync(sessionId, request, cancellationToken);
        return message is null ? NotFound() : Ok(message);
    }
}
