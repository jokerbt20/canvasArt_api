using AutoMapper;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Tags;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class TagService : ITagService
{
    private readonly ITagRepository _tags;
    private readonly IMapper _mapper;
    private readonly IDateTimeProvider _clock;

    public TagService(ITagRepository tags, IMapper mapper, IDateTimeProvider clock)
    {
        _tags = tags;
        _mapper = mapper;
        _clock = clock;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var tags = await _tags.GetAllAsync(cancellationToken);
        return _mapper.Map<List<TagDto>>(tags);
    }

    public async Task<TagDto> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        var slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, null, cancellationToken);
        var tag = new Tag { Name = request.Name.Trim(), Slug = slug, CreatedAt = _clock.UtcNow };
        tag.Id = await _tags.CreateAsync(tag, cancellationToken);
        return _mapper.Map<TagDto>(tag);
    }

    public async Task<TagDto> UpdateAsync(int id, UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        var tag = await _tags.GetByIdAsync(id, cancellationToken)
                  ?? throw new NotFoundException("Tag", id);
        tag.Slug = await EnsureUniqueSlugAsync(request.Slug, request.Name, id, cancellationToken);
        tag.Name = request.Name.Trim();
        await _tags.UpdateAsync(tag, cancellationToken);
        return _mapper.Map<TagDto>(tag);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        if (!await _tags.ExistsAsync(id, cancellationToken))
            throw new NotFoundException("Tag", id);
        await _tags.DeleteAsync(id, cancellationToken);
    }

    private async Task<string> EnsureUniqueSlugAsync(string? requested, string name, int? excludeId, CancellationToken ct)
    {
        var slug = SlugHelper.Generate(string.IsNullOrWhiteSpace(requested) ? name : requested!);
        if (string.IsNullOrEmpty(slug))
            throw new ValidationException("Unable to derive a slug from the supplied name.");
        if (await _tags.SlugExistsAsync(slug, excludeId, ct))
            throw new ConflictException($"A tag with slug '{slug}' already exists.");
        return slug;
    }
}
