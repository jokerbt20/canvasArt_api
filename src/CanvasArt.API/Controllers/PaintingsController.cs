using CanvasArt.API.Authorization;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/paintings")]
public sealed class PaintingsController : ApiControllerBase
{
    private readonly IPaintingService _paintings;

    public PaintingsController(IPaintingService paintings) => _paintings = paintings;

    // ---- Public storefront ----

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] PaintingQuery query, CancellationToken ct)
        => Success(await _paintings.QueryAsync(query, publishedOnly: true, ct));

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken ct)
        => Success(await _paintings.GetBySlugAsync(slug, incrementView: true, ct));

    // ---- Admin management ----

    [HttpGet("manage")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Query([FromQuery] PaintingQuery query, CancellationToken ct)
        => Success(await _paintings.QueryAsync(query, publishedOnly: false, ct));

    [HttpGet("manage/{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _paintings.GetByIdAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreatePaintingRequest request, CancellationToken ct)
        => Created(await _paintings.CreateAsync(request, ct), "Painting created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdatePaintingRequest request, CancellationToken ct)
        => Success(await _paintings.UpdateAsync(id, request, ct), "Painting updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _paintings.DeleteAsync(id, ct);
        return SuccessMessage("Painting deleted.");
    }

    [HttpPost("{id:int}/images")]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("An image file is required.");
        await using var stream = file.OpenReadStream();
        var result = await _paintings.UploadImageAsync(id, stream, file.FileName, ct);
        return Created(result, "Image uploaded.");
    }

    [HttpDelete("{id:int}/images/{imageId:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> DeleteImage(int id, int imageId, CancellationToken ct)
    {
        await _paintings.DeleteImageAsync(id, imageId, ct);
        return SuccessMessage("Image deleted.");
    }

    [HttpPut("{id:int}/images/{imageId:int}/primary")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> SetPrimaryImage(int id, int imageId, CancellationToken ct)
    {
        await _paintings.SetPrimaryImageAsync(id, imageId, ct);
        return SuccessMessage("Primary image updated.");
    }
}
