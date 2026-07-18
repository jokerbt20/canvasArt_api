namespace CanvasArt.API.Models.DTOs.Tags;

public record TagDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
}

public record CreateTagRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
}

public record UpdateTagRequest
{
    public string Name { get; init; } = string.Empty;
    public string? Slug { get; init; }
}
