using CanvasArt.API.Models.DTOs.FramePreviews;

namespace CanvasArt.API.Services.Interfaces;

public interface IFramePreviewService
{
    /// <summary>
    /// Returns a cached (or freshly built) framed-painting composite for the given published
    /// painting and active, compatible frame, so the customer can preview it on their own wall.
    /// </summary>
    Task<FramePreviewDto> GetOrBuildAsync(int paintingId, int frameId, int? paintingImageId, CancellationToken cancellationToken = default);
}
