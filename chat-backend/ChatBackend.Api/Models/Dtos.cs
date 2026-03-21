namespace ChatBackend.Models;

public record CreateChatSessionRequest(string Name, string Dni, string Phone, string Email);
public record CreateChatSessionResponse(Guid SessionId, string StatusMessage, bool AdvisorsAvailable);
public record SendMessageRequest(string SenderType, string SenderId, string Text);
public record ToggleAdvisorStatusRequest(bool IsActive);
public record LoginRequest(string Username, string Password);
public record LoginResponse(string Token, string AdvisorId, string AdvisorName);
public record CloseChatRequest(string ClosedBy, string? Reason);
public record TransferChatRequest(string TargetAdvisorId, string? Reason);
