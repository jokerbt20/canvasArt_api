using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Cart;
using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.DTOs.Orders;

public record CreateOrderRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public IReadOnlyList<CartLineRequest> Items { get; init; } = Array.Empty<CartLineRequest>();
}

public record OrderItemDto
{
    public int Id { get; init; }
    public int PaintingId { get; init; }
    public string PaintingCode { get; init; } = string.Empty;
    public string PaintingName { get; init; } = string.Empty;
    public string SizeLabel { get; init; } = string.Empty;
    public string? FrameName { get; init; }
    public string? ThumbnailPath { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal FramePrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public record OrderStatusHistoryDto
{
    public int Id { get; init; }
    public OrderStatus? FromStatus { get; init; }
    public OrderStatus ToStatus { get; init; }
    public string? Note { get; init; }
    public string? ChangedByName { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OrderListItemDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal GrandTotal { get; init; }
    public int ItemCount { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record OrderDetailDto
{
    public int Id { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string AddressLine { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string? Notes { get; init; }
    public OrderStatus Status { get; init; }
    public decimal SubTotal { get; init; }
    public decimal DiscountTotal { get; init; }
    public decimal ShippingCost { get; init; }
    public decimal GrandTotal { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
    public IReadOnlyList<OrderItemDto> Items { get; init; } = Array.Empty<OrderItemDto>();
    public IReadOnlyList<OrderStatusHistoryDto> History { get; init; } = Array.Empty<OrderStatusHistoryDto>();
}

public record UpdateOrderStatusRequest
{
    public OrderStatus Status { get; init; }
    public string? Note { get; init; }
}

public class OrderQuery : PagedQuery
{
    public OrderStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
