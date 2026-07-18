namespace CanvasArt.API.Models.Entities;

/// <summary>Join entity declaring that a frame may be paired with a painting.</summary>
public class FrameCompatibility
{
    public int Id { get; set; }
    public int PaintingId { get; set; }
    public int FrameId { get; set; }
    public DateTime CreatedAt { get; set; }
}
