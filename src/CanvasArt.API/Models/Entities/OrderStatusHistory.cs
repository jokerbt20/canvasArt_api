using CanvasArt.API.Models.Enums;

namespace CanvasArt.API.Models.Entities;

public class OrderStatusHistory
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public OrderStatus? FromStatus { get; set; }
    public OrderStatus ToStatus { get; set; }
    public string? Note { get; set; }
    public int? ChangedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Read projection.
    public string? ChangedByName { get; set; }
}
