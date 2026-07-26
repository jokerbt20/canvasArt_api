using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Distributors;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/promo-codes")]
public sealed class PromoCodesController : ApiControllerBase
{
    private readonly IDistributorService _distributors;

    public PromoCodesController(IDistributorService distributors) => _distributors = distributors;

    /// <summary>Storefront: validate a promo code before checkout and get its discount percentage.</summary>
    [HttpPost("apply")]
    [AllowAnonymous]
    public async Task<IActionResult> Apply(ApplyPromoCodeRequest request, CancellationToken ct)
        => Success(await _distributors.ApplyAsync(request, ct), "Promo code applied.");

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreatePromoCodeRequest request, CancellationToken ct)
        => Created(await _distributors.CreatePromoCodeAsync(request, ct), "Promo code created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdatePromoCodeRequest request, CancellationToken ct)
        => Success(await _distributors.UpdatePromoCodeAsync(id, request, ct), "Promo code updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _distributors.DeletePromoCodeAsync(id, ct);
        return SuccessMessage("Promo code deleted.");
    }
}
