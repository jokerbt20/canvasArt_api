using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/cart")]
public sealed class CartController : ApiControllerBase
{
    private readonly ICartService _cart;

    public CartController(ICartService cart) => _cart = cart;

    /// <summary>Prices a guest cart (painting + size + optional frame) applying active promotions.</summary>
    [HttpPost("calculate")]
    [AllowAnonymous]
    public async Task<IActionResult> Calculate(CartRequest request, CancellationToken ct)
        => Success(await _cart.CalculateAsync(request, ct));
}
