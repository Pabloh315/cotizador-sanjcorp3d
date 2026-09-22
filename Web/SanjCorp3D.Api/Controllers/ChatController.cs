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
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] long? after, CancellationToken ct)
    {
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var query = db.ChatMessages.AsNoTracking().Where(x => x.TenantId == tenantId);
        if (after is > 0) query = query.Where(x => x.Id > after.Value);
        return Ok(await query.OrderBy(x => x.CreatedAtUtc).Take(200).ToListAsync(ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateChatMessageRequest request, CancellationToken ct)
    {
        var tenantId = tenantContext.CurrentTenantId ?? throw new InvalidOperationException("No se seleccionó un espacio de trabajo.");
        var user = await users.GetUserAsync(User);
        if (user is null || !user.Active) return Unauthorized();
        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length == 0 && string.IsNullOrWhiteSpace(request.PhotoUrl))
            return BadRequest(new { message = "Escribe un mensaje o adjunta una foto." });
        if (body.Length > 4000) return BadRequest(new { message = "El mensaje es demasiado largo." });
        if (request.PhotoUrl is { Length: > 2_000_000 }) return BadRequest(new { message = "La foto no puede superar 2 MB." });
        var message = new ChatMessage
        {
            TenantId = tenantId, SenderUserId = user.Id,
            SenderName = string.IsNullOrWhiteSpace(user.DisplayName) ? user.UserName ?? "Usuario" : user.DisplayName,
            Body = body, PhotoUrl = string.IsNullOrWhiteSpace(request.PhotoUrl) ? null : request.PhotoUrl
        };
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
        return Created($"/api/chat/{message.Id}", message);
    }
}

