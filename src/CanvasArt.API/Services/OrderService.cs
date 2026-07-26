using System.Globalization;
using AutoMapper;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Orders;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Settings;
using CanvasArt.API.Models;
using CanvasArt.API.Models.Entities;
using CanvasArt.API.Models.Enums;
using Microsoft.Extensions.Options;

namespace CanvasArt.API.Services;

public sealed class OrderService : IOrderService
{
    private const string ShippingSettingKey = "shipping.flat_rate";

    private readonly IOrderRepository _orders;
    private readonly ISettingRepository _settings;
    private readonly CartPricer _pricer;
    private readonly IDistributorService _distributors;
    private readonly IEmailService _email;
    private readonly EmailSettings _emailSettings;
    private readonly IImageService _images;
    private readonly IMapper _mapper;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _clock;

    public OrderService(
        IOrderRepository orders,
        ISettingRepository settings,
        CartPricer pricer,
        IDistributorService distributors,
        IEmailService email,
        IOptions<EmailSettings> emailSettings,
        IImageService images,
        IMapper mapper,
        ICurrentUserService currentUser,
        IDateTimeProvider clock)
    {
        _orders = orders;
        _settings = settings;
        _pricer = pricer;
        _distributors = distributors;
        _email = email;
        _emailSettings = emailSettings.Value;
        _images = images;
        _mapper = mapper;
        _currentUser = currentUser;
        _clock = clock;
    }

    public async Task<OrderDetailDto> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken = default)
    {
        var lines = await _pricer.PriceAsync(request.Items, cancellationToken);

        var shipping = await ResolveShippingAsync(cancellationToken);
        var subTotal = Math.Round(lines.Sum(l => l.LineSubTotal), 2, MidpointRounding.AwayFromZero);
        var discountTotal = Math.Round(lines.Sum(l => l.LineDiscount), 2, MidpointRounding.AwayFromZero);
        var lineTotals = Math.Round(lines.Sum(l => l.LineTotal), 2, MidpointRounding.AwayFromZero);

        // Optional distributor promo code: an extra percentage off the already-discounted goods
        // total (automatic promotions apply first). Shipping is never discounted.
        var promo = await _distributors.ResolveForOrderAsync(request.PromoCode, cancellationToken);
        var promoDiscount = promo is null
            ? 0m
            : Math.Round(lineTotals * promo.DiscountPercentage / 100m, 2, MidpointRounding.AwayFromZero);

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
            GrandTotal = lineTotals - promoDiscount + shipping,
            PromoCodeId = promo?.PromoCodeId,
            DistributorId = promo?.DistributorId,
            PromoCode = promo?.Code,
            DistributorName = promo?.DistributorName,
            PromoDiscount = promoDiscount,
            CreatedAt = now,
            UpdatedAt = now
        };

        var items = lines.Select(l => new OrderItem
        {
            PaintingId = l.PaintingId,
            PaintingSizeId = l.PaintingSizeId,
            FrameId = l.FrameId,
            PaintingCode = l.PaintingCode,
            PaintingName = l.PaintingName,
            SizeLabel = l.SizeLabel,
            FrameName = l.FrameName,
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
        var detail = await GetByIdAsync(id, cancellationToken);

        // Fire-and-forget: the customer must never wait on SMTP. Only singletons and the already
        // materialised DTO are captured, so it is safe to run past the request scope. Uses
        // CancellationToken.None so completing the response does not cancel the send.
        _ = SendOrderEmailsAsync(detail);

        return detail;
    }

    /// <summary>
    /// Sends the customer confirmation and the shop summary. <see cref="IEmailService"/> swallows
    /// its own failures, so a mail outage never breaks order creation.
    /// </summary>
    private async Task SendOrderEmailsAsync(OrderDetailDto order)
    {
        var customerSubject = $"Нарачка / Order {order.OrderNumber} · CanvasArts";
        await _email.SendAsync(order.Email, customerSubject, OrderEmailComposer.BuildCustomerEmail(order), isHtml: true, cancellationToken: CancellationToken.None);

        var notifyTo = _emailSettings.NotifyToAddress;
        if (!string.IsNullOrWhiteSpace(notifyTo))
        {
            var businessSubject = $"Нова нарачка / New order {order.OrderNumber} — {order.FirstName} {order.LastName}";
            await _email.SendAsync(notifyTo, businessSubject, OrderEmailComposer.BuildBusinessEmail(order), isHtml: true, cancellationToken: CancellationToken.None);
        }
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
            PromoDiscount = o.PromoDiscount,
            ShippingCost = o.ShippingCost,
            GrandTotal = o.GrandTotal,
            PromoCode = o.PromoCode,
            DistributorName = o.DistributorName,
            CreatedAt = o.CreatedAt,
            UpdatedAt = o.UpdatedAt,
            Items = a.Items.Select(ToItemDto).ToList(),
            History = _mapper.Map<List<OrderStatusHistoryDto>>(a.History)
        };
    }

    /// <summary>
    /// Maps a stored order item, turning the raw painting thumbnail and the (join-supplied) frame
    /// thumbnail filenames into fully-qualified public URLs the frontend can render directly.
    /// </summary>
    private OrderItemDto ToItemDto(OrderItem i) => new()
    {
        Id = i.Id,
        PaintingId = i.PaintingId,
        PaintingCode = i.PaintingCode,
        PaintingName = i.PaintingName,
        SizeLabel = i.SizeLabel,
        FrameName = i.FrameName,
        ThumbnailPath = _images.BuildThumbUrl(i.ThumbnailPath),
        FrameThumbnailPath = _images.BuildFrameUrl(i.FrameThumbnailPath),
        UnitPrice = i.UnitPrice,
        FramePrice = i.FramePrice,
        DiscountAmount = i.DiscountAmount,
        Quantity = i.Quantity,
        LineTotal = i.LineTotal
    };
}
