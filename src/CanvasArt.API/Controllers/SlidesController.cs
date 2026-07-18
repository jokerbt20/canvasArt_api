using CanvasArt.API.Authorization;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/slides")]
public sealed class SlidesController : ApiControllerBase
{
    private readonly ICmsService _cms;

    public SlidesController(ICmsService cms) => _cms = cms;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Success(await _cms.GetSlidesAsync(activeOnly: true, ct));

    [HttpGet("manage")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Success(await _cms.GetSlidesAsync(activeOnly: false, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _cms.GetSlideAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateSlideRequest request, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("A slide image is required.");
        await using var stream = file.OpenReadStream();
        return Created(await _cms.CreateSlideAsync(request, stream, file.FileName, ct), "Slide created.");
    }

    [HttpPost("from-painting-image")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> CreateFromPaintingImage(CreateSlideFromPaintingImageRequest request, CancellationToken ct)
        => Created(await _cms.CreateSlideFromPaintingImageAsync(request, ct), "Slide created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateSlideRequest request, IFormFile? file, CancellationToken ct)
    {
        if (file is not null && file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            return Success(await _cms.UpdateSlideAsync(id, request, stream, file.FileName, ct), "Slide updated.");
        }
        return Success(await _cms.UpdateSlideAsync(id, request, null, null, ct), "Slide updated.");
    }

    [HttpPut("{id:int}/from-painting-image")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> UpdateFromPaintingImage(int id, CreateSlideFromPaintingImageRequest request, CancellationToken ct)
        => Success(await _cms.UpdateSlideFromPaintingImageAsync(id, request, ct), "Slide updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _cms.DeleteSlideAsync(id, ct);
        return SuccessMessage("Slide deleted.");
    }
}
