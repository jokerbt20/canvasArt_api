using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface ISlideRepository
{
    Task<IReadOnlyList<Slide>> GetAllAsync(bool activeOnly, DateTime nowUtc, CancellationToken cancellationToken = default);
    Task<Slide?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Slide slide, CancellationToken cancellationToken = default);
    Task UpdateAsync(Slide slide, CancellationToken cancellationToken = default);
    Task UpdateImageAsync(int id, string imagePath, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}

public interface ISettingRepository
{
    Task<IReadOnlyList<Setting>> GetAllAsync(string? group, CancellationToken cancellationToken = default);
    Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default);
    Task UpsertAsync(Setting setting, CancellationToken cancellationToken = default);
    Task UpsertManyAsync(IReadOnlyList<Setting> settings, CancellationToken cancellationToken = default);
}

public interface ITestimonialRepository
{
    Task<IReadOnlyList<Testimonial>> GetAllAsync(bool activeOnly, CancellationToken cancellationToken = default);
    Task<Testimonial?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<int> CreateAsync(Testimonial testimonial, CancellationToken cancellationToken = default);
    Task UpdateAsync(Testimonial testimonial, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
