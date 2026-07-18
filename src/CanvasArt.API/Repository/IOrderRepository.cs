using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Repository;

public interface IOrderRepository
{
    Task<PagedResult<OrderListItemDto>> QueryAsync(OrderQuery query, CancellationToken cancellationToken = default);
    Task<OrderAggregate?> GetAggregateByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<OrderAggregate?> GetAggregateByNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists the order, its items and the initial status-history row in a single transaction.
    /// Returns the new order id.
    /// </summary>
    Task<int> CreateAsync(Order order, IReadOnlyList<OrderItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates status and appends a history row atomically. Returns false if the order was not found.
    /// </summary>
    Task<bool> ChangeStatusAsync(int orderId, OrderStatus newStatus, string? note, int? changedByUserId, DateTime whenUtc, CancellationToken cancellationToken = default);

    Task<OrderStatsDto> GetStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Dashboard counters for the admin.</summary>
public sealed record OrderStatsDto
{
    public long Total { get; init; }
    public long Pending { get; init; }
    public long Processing { get; init; }
    public long Delivered { get; init; }
    public long Cancelled { get; init; }
    public decimal RevenueDelivered { get; init; }
}
