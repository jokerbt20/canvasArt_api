using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/settings")]
public sealed class SettingsController : ApiControllerBase
{
    private readonly ICmsService _cms;

    public SettingsController(ICmsService cms) => _cms = cms;

    /// <summary>Public read of CMS settings (footer, contact, social links, about, …), optionally by group.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] string? group, CancellationToken ct)
        => Success(await _cms.GetSettingsAsync(group, ct));

    [HttpPut]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Upsert(UpsertSettingsRequest request, CancellationToken ct)
    {
        await _cms.UpsertSettingsAsync(request, ct);
        return SuccessMessage("Settings saved.");
    }
}
