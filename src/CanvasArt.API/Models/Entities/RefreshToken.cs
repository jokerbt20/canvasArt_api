namespace CanvasArt.API.Models.Entities;

public class RefreshToken
{
    public long Id { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public string JwtId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public bool IsActive => !IsRevoked && !IsUsed && DateTime.UtcNow < ExpiresAt;
}
