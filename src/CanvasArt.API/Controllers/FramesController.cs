using CanvasArt.API.Authorization;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Frames;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/frames")]
public sealed class FramesController : ApiControllerBase
{
    private readonly IFrameService _frames;

    public FramesController(IFrameService frames) => _frames = frames;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> List([FromQuery] FrameQuery query, CancellationToken ct)
        => Success(await _frames.QueryAsync(query, activeOnly: true, ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _frames.GetByIdAsync(id, ct));

    [HttpGet("manage")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Query([FromQuery] FrameQuery query, CancellationToken ct)
        => Success(await _frames.QueryAsync(query, activeOnly: false, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreateFrameRequest request, CancellationToken ct)
        => Created(await _frames.CreateAsync(request, ct), "Frame created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdateFrameRequest request, CancellationToken ct)
        => Success(await _frames.UpdateAsync(id, request, ct), "Frame updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _frames.DeleteAsync(id, ct);
        return SuccessMessage("Frame deleted.");
    }

    [HttpPost("{id:int}/image")]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> UploadImage(int id, IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new ValidationException("An image file is required.");
        await using var stream = file.OpenReadStream();
        return Success(await _frames.UploadImageAsync(id, stream, file.FileName, ct), "Frame image updated.");
    }
}
