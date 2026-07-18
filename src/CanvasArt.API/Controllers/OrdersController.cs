using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/orders")]
public sealed class OrdersController : ApiControllerBase
{
    private readonly IOrderService _orders;

    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Guest checkout — creates an order from a priced cart.</summary>
    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Create(CreateOrderRequest request, CancellationToken ct)
        => Created(await _orders.CreateAsync(request, ct), "Order placed successfully.");

    /// <summary>Public order tracking by order number.</summary>
    [HttpGet("track/{orderNumber}")]
    [AllowAnonymous]
    public async Task<IActionResult> Track(string orderNumber, CancellationToken ct)
        => Success(await _orders.GetByNumberAsync(orderNumber, ct));

    // ---- Admin ----

    [HttpGet]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Query([FromQuery] OrderQuery query, CancellationToken ct)
        => Success(await _orders.QueryAsync(query, ct));

    [HttpGet("stats")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Stats(CancellationToken ct)
        => Success(await _orders.GetStatsAsync(ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _orders.GetByIdAsync(id, ct));

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> UpdateStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
        => Success(await _orders.UpdateStatusAsync(id, request, ct), "Order status updated.");
}
