using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IFrameRepository
{
    Task<PagedResult<FrameListItemDto>> QueryAsync(FrameQuery query, bool activeOnly, CancellationToken cancellationToken = default);
    Task<Frame?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Frame>> GetCompatibleFramesAsync(int paintingId, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> AllExistAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    Task<int> CreateAsync(Frame frame, CancellationToken cancellationToken = default);
    Task UpdateAsync(Frame frame, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    Task UpdateImagesAsync(int frameId, string imagePath, string thumbnailPath, CancellationToken cancellationToken = default);

    Task<bool> IsCompatibleAsync(int paintingId, int frameId, CancellationToken cancellationToken = default);
}
