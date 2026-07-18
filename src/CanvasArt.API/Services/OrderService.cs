using System.Globalization;
using AutoMapper;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Services;

public sealed class OrderService : IOrderService
{
    private const string ShippingSettingKey = "shipping.flat_rate";

    private readonly IOrderRepository _orders;
    private readonly ISettingRepository _settings;
    private readonly CartPricer _pricer;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public OrderService(
        IOrderRepository orders,
        ISettingRepository settings,
        CartPricer pricer,
        IMapper mapper,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _settings = settings;
        _pricer = pricer;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var lines = await _pricer.PriceAsync(request.Items, enforceStock: true, cancellationToken);

        var shipping = await ResolveShippingAsync(cancellationToken);
        var subTotal = Math.Round(lines.Sum(l => l.LineSubTotal), 2, MidpointRounding.AwayFromZero);
        var discountTotal = Math.Round(lines.Sum(l => l.LineDiscount), 2, MidpointRounding.AwayFromZero);
        var lineTotals = Math.Round(lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

        var now = _clock.UtcNow;
        var order = new Order
        {
            OrderNumber = GenerateOrderNumber(now),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.Trim(),
            Phone = request.Phone.Trim(),
            AddressLine = request.AddressLine.Trim(),
            City = request.City.Trim(),
            Country = request.Country.Trim(),
            PostalCode = request.PostalCode.Trim(),
            Notes = request.Notes?.Trim(),
            Status = OrderStatus.Pending,
            SubTotal = subTotal,
            DiscountTotal = discountTotal,
            ShippingCost = shipping,
            GrandTotal = lineTotals + shipping,
            CreatedAt = now,
            UpdatedAt = now
        };

        var items = lines.Select(l => new OrderItem
        {
            PaintingId = l.PaintingId,
            PaintingSizeId = l.PaintingSizeId,
            FrameId = l.FrameId,
            FrameSizeId = l.FrameSizeId,
            PaintingCode = l.PaintingCode,
            PaintingName = l.PaintingName,
            SizeLabel = l.SizeLabel,
            FrameName = l.FrameName,
            FrameSizeLabel = l.FrameSizeLabel,
            ThumbnailPath = l.ThumbnailPath,
            UnitPrice = l.PaintingBasePrice,
            FramePrice = l.FrameBasePrice,
            DiscountAmount = l.LineDiscount,
            Quantity = l.Quantity,
            LineTotal = l.LineTotal,
            AppliedPromotionId = l.AppliedPromotionId,
            AppliedCombinationPromotionId = l.AppliedCombinationPromotionId
        }).ToList();

        var id = await _orders.CreateAsync(order, items, cancellationToken);
        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<PagedResult<OrderListItemDto>> QueryAsync(OrderQuery query, CancellationToken cancellationToken = default) =>
        _orders.QueryAsync(query, cancellationToken);

    public async Task<OrderDetailDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var aggregate = await _orders.GetAggregateByIdAsync(id, cancellationToken)
                        ?? throw new NotFoundException("Order", id);
        return ToDetail(aggregate);
    }

    public async Task<OrderDetailDto> GetByNumberAsync(string orderNumber, CancellationToken cancellationToken = default)
    {
        var aggregate = await _orders.GetAggregateByNumberAsync(orderNumber, cancellationToken)
                        ?? throw new NotFoundException($"Order '{orderNumber}' was not found.");
        return ToDetail(aggregate);
    }

    public async Task<OrderDetailDto> UpdateStatusAsync(int id, UpdateOrderStatusRequest request, CancellationToken cancellationToken = default)
    {
        var order = await _orders.GetByIdAsync(id, cancellationToken)
                    ?? throw new NotFoundException("Order", id);

        if (order.Status == request.Status)
            throw new ValidationException($"The order is already '{request.Status}'.");
        if (order.Status is OrderStatus.Delivered or OrderStatus.Cancelled)
            throw new ConflictException($"A '{order.Status}' order can no longer change status.");

        var changed = await _orders.ChangeStatusAsync(
            id, request.Status, request.Note?.Trim(), _currentUser.UserId, _clock.UtcNow, cancellationToken);
        if (!changed)
            throw new NotFoundException("Order", id);

        return await GetByIdAsync(id, cancellationToken);
    }

    public Task<OrderStatsDto> GetStatsAsync(CancellationToken cancellationToken = default) =>
        _orders.GetStatsAsync(cancellationToken);

    private async Task<decimal> ResolveShippingAsync(CancellationToken ct)
    {
        var setting = await _settings.GetByKeyAsync(ShippingSettingKey, ct);
        if (setting?.Value is { Length: > 0 } value &&
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) && rate > 0)
        {
            return Math.Round(rate, 2, MidpointRounding.AwayFromZero);
        }
        return 0m;
    }

    private static string GenerateOrderNumber(DateTime now) =>
        $"ORD-{now:yyyyMMddHHmmss}-{Random.Shared.Next(0, 10_000):D4}";

    private OrderDetailDto ToDetail(OrderAggregate a)
    {
        var o = a.Order;
        return new OrderDetailDto
        {
            Id = o.Id,
            OrderNumber = o.OrderNumber,
            FirstName = o.FirstName,
            LastName = o.LastName,
            Email = o.Email,
            Phone = o.Phone,
            AddressLine = o.AddressLine,
            City = o.City,
            Country = o.Country,
            PostalCode = o.PostalCode,
            Notes = o.Notes,
            Status = o.Status,
            SubTotal = o.SubTotal,
            DiscountTotal = o.DiscountTotal,
            ShippingCost = o.ShippingCost,
            GrandTotal = o.GrandTotal,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = _mapper.Map<List<OrderItemDto>>(a.Items),
            History = _mapper.Map<List<OrderStatusHistoryDto>>(a.History)
        };
    }
}
