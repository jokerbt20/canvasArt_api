using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Models.DTOs.Contact;

public record ContactMessageDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
}

public record CreateContactMessageRequest
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Subject { get; init; }
    public string Message { get; init; } = string.Empty;
}

public class ContactMessageQuery : PagedQuery
{
    public bool? IsRead { get; set; }
}
