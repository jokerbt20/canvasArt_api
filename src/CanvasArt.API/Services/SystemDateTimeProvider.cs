using CanvasArt.API.Services.Interfaces;

namespace CanvasArt.API.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
