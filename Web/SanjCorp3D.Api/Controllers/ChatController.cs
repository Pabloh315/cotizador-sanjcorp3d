using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SanjCorp3D.Api.Contracts;
using SanjCorp3D.Api.Data;
using SanjCorp3D.Api.Identity;
using SanjCorp3D.Api.Models;
using SanjCorp3D.Api.Services;

namespace SanjCorp3D.Api.Controllers;

[ApiController, Authorize, Route("api/chat")]
public sealed class ChatController(AppDbContext db, UserManager<ApplicationUser> users, TenantContext tenantContext) : ControllerBase
{
    private const int MaxAttachmentCharacters = 7_000_000;
    private static readonly HashSet<string> AllowedAttachmentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png", "image/jpeg", "application/pdf", "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    [HttpGet("conversations")]
    [Authorize(Roles = AppRoles.SuperAdmin)]
    public async Task<IActionResult> Conversations(CancellationToken ct)
    {
        var tenants = await db.Tenants.AsNoTracking().Where(x => x.Kind == "maker" && x.Active).OrderBy(x => x.Name).ToListAsync(ct);
        var tenantIds = tenants.Select(tenant => tenant.Id).ToArray();
        var lastMessages = await db.ChatMessages.IgnoreQueryFilters().Where(x => tenantIds.Contains(x.TenantId))
            .GroupBy(x => x.TenantId).Select(group => new { TenantId = group.Key, LastMessageAtUtc = group.Max(item => item.CreatedAtUtc) })
            .ToDictionaryAsync(item => item.TenantId, item => (DateTime?)item.LastMessageAtUtc, ct);
        return Ok(tenants.Select(tenant => new SupportConversationDto(tenant.Id, tenant.Name, tenant.LogoUrl, lastMessages.GetValueOrDefault(tenant.Id))).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? tenantId, [FromQuery] long? after, CancellationToken ct)
    {
        var user = await CurrentActiveUser();
        if (user is null) return Unauthorized();
        var conversationTenantId = await ResolveConversationTenant(user, tenantId, ct);
        if (conversationTenantId is null) return Forbid();
        var query = db.ChatMessages.IgnoreQueryFilters().AsNoTracking().Where(x => x.TenantId == conversationTenantId.Value);
        if (after is > 0) query = query.Where(x => x.Id > after.Value);
        return Ok(await query.OrderBy(x => x.CreatedAtUtc).Take(300).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateChatMessageRequest request, CancellationToken ct)
    {
        var user = await CurrentActiveUser();
        if (user is null) return Unauthorized();
        var conversationTenantId = await ResolveConversationTenant(user, request.TenantId, ct);
        if (conversationTenantId is null) return Forbid();
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length == 0 && string.IsNullOrWhiteSpace(request.AttachmentUrl)) return BadRequest(new { message = "Escribe un mensaje o adjunta un archivo." });
        if (body.Length > 4000) return BadRequest(new { message = "El mensaje es demasiado largo." });
        var attachmentError = ValidateAttachment(request);
        if (attachmentError is not null) return BadRequest(new { message = attachmentError });

        tenantContext.Use(conversationTenantId.Value);
        var message = new ChatMessage
        {
            TenantId = conversationTenantId.Value, SenderUserId = user.Id,
            SenderName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "Usuario" : user.DisplayName,
            Body = body, PhotoUrl = string.IsNullOrWhiteSpace(request.AttachmentUrl) ? null : request.AttachmentUrl,
            AttachmentName = string.IsNullOrWhiteSpace(request.AttachmentName) ? null : Path.GetFileName(request.AttachmentName.Trim()),
            AttachmentContentType = string.IsNullOrWhiteSpace(request.AttachmentContentType) ? null : request.AttachmentContentType.Trim().ToLowerInvariant()
        };
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return Created($"/api/chat/{message.Id}", message);
    }

    private async Task<ApplicationUser?> CurrentActiveUser()
    {
        var user = await users.GetUserAsync(User);
        return user is { Active: true } ? user : null;
    }

    private async Task<Guid?> ResolveConversationTenant(ApplicationUser user, Guid? requestedTenantId, CancellationToken ct)
    {
        if (User.IsInRole(AppRoles.SuperAdmin))
        {
            if (requestedTenantId is null) return null;
            return await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == requestedTenantId.Value && x.Kind == "maker" && x.Active, ct) ? requestedTenantId : null;
        }
        if (!await users.IsInRoleAsync(user, AppRoles.Administrator)) return null;
        var tenantId = tenantContext.CurrentTenantId;
        return tenantId is not null && await db.Tenants.IgnoreQueryFilters().AnyAsync(x => x.Id == tenantId.Value && x.Kind == "maker" && x.Active, ct) ? tenantId : null;
    }

    private static string? ValidateAttachment(CreateChatMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.AttachmentUrl)) return null;
        if (request.AttachmentUrl.Length > MaxAttachmentCharacters) return "El archivo no puede superar 5 MB.";
        if (string.IsNullOrWhiteSpace(request.AttachmentName) || request.AttachmentName.Trim().Length > 160) return "El archivo debe tener un nombre valido.";
        if (string.IsNullOrWhiteSpace(request.AttachmentContentType) || !AllowedAttachmentTypes.Contains(request.AttachmentContentType)) return "Solo se permiten archivos PNG, JPG, PDF, DOC y DOCX.";
        var prefix = $"data:{request.AttachmentContentType};base64,";
        return request.AttachmentUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ? null : "El archivo adjunto no tiene un formato valido.";
    }
}