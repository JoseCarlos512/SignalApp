using System.Security.Claims;
using ChatBackend.Hubs;
using ChatBackend.Models;
using ChatBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace ChatBackend.Controllers;

[ApiController]
[Route("api/chats")]
public class ChatController(IChatService chatService, IHubContext<ChatHub> hubContext) : ControllerBase
{
    [HttpPost("session")]
    public async Task<ActionResult<CreateChatSessionResponse>> CreateSession([FromBody] CreateChatSessionRequest request)
    {
        var session = chatService.CreateSession(request);
        var advisorsAvailable = chatService.HasActiveAdvisors();
        var statusMessage = advisorsAvailable
            ? "Conectando con un asesor..."
            : "No hay asesores disponibles en este momento";

        await hubContext.Clients.Group("advisors").SendAsync("newIncomingChat", new
        {
            session.Id,
            session.Applicant,
            session.CreatedAt,
            session.Status,
            session.AssignedAdvisorId,
            statusMessage
        });

        return Ok(new CreateChatSessionResponse(session.Id, statusMessage, advisorsAvailable));
    }

    [Authorize]
    [HttpGet("pending")]
    public ActionResult<IReadOnlyCollection<ChatSession>> GetPendingChats() => Ok(chatService.GetPendingChats());

    [Authorize]
    [HttpGet]
    public ActionResult<IReadOnlyCollection<ChatSession>> GetAllChats()
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return Ok(chatService.GetAllChats(advisorId));
    }


    [Authorize]
    [HttpGet("advisors")]
    public ActionResult<IReadOnlyCollection<AdvisorState>> GetAdvisors() => Ok(chatService.GetAdvisors());

    [Authorize]
    [HttpPatch("advisor/active")]
    public async Task<IActionResult> SetAdvisorActive([FromBody] ToggleAdvisorStatusRequest request)
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var advisorName = User.FindFirstValue(ClaimTypes.Name)!;

        chatService.SetAdvisorActive(advisorId, advisorName, request.IsActive);

        await hubContext.Clients.Group("advisors").SendAsync("advisorStatusChanged", new
        {
            advisorId,
            isActive = request.IsActive
        });

        return NoContent();
    }

    [Authorize]
    [HttpPost("{sessionId:guid}/take")]
    public async Task<IActionResult> TakeChat(Guid sessionId)
    {
        var advisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var chat = chatService.TakeChat(sessionId, advisorId);

        if (chat is null)
        {
            return NotFound("Chat no encontrado o ya tomado.");
        }

        var room = $"chat-{sessionId}";
        await hubContext.Clients.Group(room).SendAsync("chatTaken", new { sessionId, advisorId });
        await hubContext.Clients.Group(room).SendAsync("chatHistory", chat.Messages);
        await hubContext.Clients.Group("advisors").SendAsync("chatUpdated", new
        {
            sessionId,
            status = chat.Status.ToString(),
            assignedAdvisorId = chat.AssignedAdvisorId
        });
        await hubContext.Clients.Group($"advisor-{advisorId}").SendAsync("assignmentNotification", new
        {
            sessionId,
            message = "Se te asignó una nueva solicitud."
        });

        return Ok(chat);
    }

    [Authorize]
    [HttpPost("{sessionId:guid}/transfer")]
    public async Task<IActionResult> TransferChat(Guid sessionId, [FromBody] TransferChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TargetAdvisorId))
        {
            return BadRequest("Debes indicar el asesor destino.");
        }

        var sourceAdvisorId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var advisorName = User.FindFirstValue(ClaimTypes.Name) ?? "asesor";
        var chat = chatService.TransferChat(sessionId, sourceAdvisorId, request.TargetAdvisorId, advisorName, request.Reason);

        if (chat is null)
        {
            return Conflict("No se pudo derivar. Verifica que el chat siga asignado a tu usuario y que el asesor destino esté activo.");
        }

        await hubContext.Clients.Group("advisors").SendAsync("chatUpdated", new
        {
            sessionId,
            status = chat.Status.ToString(),
            assignedAdvisorId = chat.AssignedAdvisorId
        });

        await hubContext.Clients.Group($"advisor-{request.TargetAdvisorId}").SendAsync("assignmentNotification", new
        {
            sessionId,
            message = $"Se te asignó la solicitud {sessionId}."
        });

        await hubContext.Clients.Group($"chat-{sessionId}").SendAsync("newMessage", chat.Messages.Last());
        return Ok(chat);
    }

    [HttpPost("{sessionId:guid}/close")]
    public async Task<IActionResult> CloseChat(Guid sessionId, [FromBody] CloseChatRequest request)
    {
        var closedBy = string.IsNullOrWhiteSpace(request.ClosedBy) ? "usuario" : request.ClosedBy;
        var chat = chatService.CloseChat(sessionId, closedBy, request.Reason);

        if (chat is null)
        {
            return NotFound("Chat no encontrado o ya cerrado.");
        }

        var room = $"chat-{sessionId}";
        await hubContext.Clients.Group(room).SendAsync("chatClosed", new
        {
            sessionId,
            closedBy,
            reason = request.Reason
        });

        await hubContext.Clients.Group(room).SendAsync("newMessage", chat.Messages.Last());
        await hubContext.Clients.Group("advisors").SendAsync("chatUpdated", new
        {
            sessionId,
            status = chat.Status.ToString(),
            assignedAdvisorId = chat.AssignedAdvisorId
        });

        return Ok(chat);
    }

    [HttpGet("{sessionId:guid}")]
    public ActionResult<ChatSession> GetSession(Guid sessionId)
    {
        var chat = chatService.GetChat(sessionId);
        return chat is null ? NotFound() : Ok(chat);
    }

    [HttpPost("{sessionId:guid}/messages")]
    public async Task<IActionResult> SendMessage(Guid sessionId, [FromBody] SendMessageRequest request)
    {
        var message = chatService.AddMessage(sessionId, request.SenderType, request.SenderId, request.Text);
        if (message is null)
        {
            return NotFound();
        }

        await hubContext.Clients.Group($"chat-{sessionId}").SendAsync("newMessage", message);
        return Ok(message);
    }
}
