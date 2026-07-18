namespace CanvasArt.API.Services.Interfaces;

/// <summary>Abstraction over the system clock for testability. Always returns UTC.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
