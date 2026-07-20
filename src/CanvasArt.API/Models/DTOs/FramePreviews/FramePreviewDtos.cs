namespace CanvasArt.API.Models.DTOs.FramePreviews;

/// <summary>A server-composited framed-painting image, ready for client-side room compositing.</summary>
public record FramePreviewDto
{
    public string PreviewUrl { get; init; } = string.Empty;
    public int Width { get; init; }
    public int Height { get; init; }
}
