namespace CanvasArt.API.Services.Interfaces;

/// <summary>Exposes the identity of the caller for the current request.</summary>
public interface ICurrentUserService
{
    int? UserId { get; }
    string? Email { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}
