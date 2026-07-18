using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Promotions;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/promotions")]
public sealed class PromotionsController : ApiControllerBase
{
    private readonly IPromotionService _promotions;

    public PromotionsController(IPromotionService promotions) => _promotions = promotions;

    /// <summary>Public read, used by both the admin manage screen and the storefront offers page (filtered via onlyCurrentlyActive).</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Query([FromQuery] PromotionQuery query, CancellationToken ct)
        => Success(await _promotions.QueryAsync(query, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _promotions.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreatePromotionRequest request, CancellationToken ct)
        => Created(await _promotions.CreateAsync(request, ct), "Promotion created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdatePromotionRequest request, CancellationToken ct)
        => Success(await _promotions.UpdateAsync(id, request, ct), "Promotion updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _promotions.DeleteAsync(id, ct);
        return SuccessMessage("Promotion deleted.");
    }

    // ---- Combination promotions ----

    /// <summary>Public read, used by both the admin manage screen and the storefront offers page (filtered via onlyCurrentlyActive).</summary>
    [HttpGet("combinations")]
    [AllowAnonymous]
    public async Task<IActionResult> QueryCombinations([FromQuery] PromotionQuery query, CancellationToken ct)
        => Success(await _promotions.QueryCombinationsAsync(query, ct));

    [HttpGet("combinations/{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetCombination(int id, CancellationToken ct)
        => Success(await _promotions.GetCombinationByIdAsync(id, ct));

    [HttpPost("combinations")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> CreateCombination(CreateCombinationPromotionRequest request, CancellationToken ct)
        => Created(await _promotions.CreateCombinationAsync(request, ct), "Combination promotion created.");

    [HttpPut("combinations/{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> UpdateCombination(int id, UpdateCombinationPromotionRequest request, CancellationToken ct)
        => Success(await _promotions.UpdateCombinationAsync(id, request, ct), "Combination promotion updated.");

    [HttpDelete("combinations/{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> DeleteCombination(int id, CancellationToken ct)
    {
        await _promotions.DeleteCombinationAsync(id, ct);
        return SuccessMessage("Combination promotion deleted.");
    }
}
