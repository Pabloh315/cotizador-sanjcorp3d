namespace SanjCorp3D.Api.Contracts;

public sealed record CreateChatMessageRequest(string Body, string? AttachmentUrl, string? AttachmentName, string? AttachmentContentType, Guid? TenantId = null);
public sealed record SupportConversationDto(Guid Id, string Name, string? LogoUrl, DateTime? LastMessageAtUtc);