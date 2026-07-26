namespace CanvasArt.API.Models.Entities;

/// <summary>
/// A sales distributor. Each distributor owns one or more <see cref="PromoCode"/>s that carry an
/// extra discount for the customer and attribute the resulting sale back to the distributor.
/// </summary>
public class Distributor
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
