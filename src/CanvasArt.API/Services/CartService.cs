using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Services.Interfaces;

namespace CanvasArt.API.Services;

public sealed class CartService : ICartService
{
    private readonly CartPricer _pricer;

    public CartService(CartPricer pricer) => _pricer = pricer;

    public async Task<CartResponse> CalculateAsync(CartRequest request, CancellationToken cancellationToken = default)
    {
        var lines = await _pricer.PriceAsync(request.Items, cancellationToken);
        return CartPricer.ToCartResponse(lines);
    }
}
