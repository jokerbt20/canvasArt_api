using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Contact;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Repository;

public interface IContactMessageRepository
{
    Task<int> CreateAsync(ContactMessage message, CancellationToken cancellationToken = default);
    Task<PagedResult<ContactMessage>> QueryAsync(ContactMessageQuery query, CancellationToken cancellationToken = default);
    Task<ContactMessage?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task MarkReadAsync(int id, CancellationToken cancellationToken = default);
}
