using System.Security.Claims;
using ChatBackend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatBackend.Hubs;

public class ChatHub(IChatService chatService) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role == "advisor")
        {
            var advisorId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            var advisorName = Context.User?.FindFirstValue(ClaimTypes.Name) ?? advisorId;
            chatService.AddConnection(advisorId, advisorName, Context.ConnectionId);
            await Groups.AddToGroupAsync(Context.ConnectionId, "advisors");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var role = Context.User?.FindFirstValue(ClaimTypes.Role);
        if (role == "advisor")
        {
            var advisorId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";
            chatService.RemoveConnection(advisorId, Context.ConnectionId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "advisors");
        }

        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChatRoom(string room) => await Groups.AddToGroupAsync(Context.ConnectionId, room);
    public async Task LeaveChatRoom(string room) => await Groups.RemoveFromGroupAsync(Context.ConnectionId, room);
}
