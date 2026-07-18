using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Tags;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/tags")]
public sealed class TagsController : ApiControllerBase
{
    private readonly ITagService _tags;

    public TagsController(ITagService tags) => _tags = tags;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Success(await _tags.GetAllAsync(ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreateTagRequest request, CancellationToken ct)
        => Created(await _tags.CreateAsync(request, ct), "Tag created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdateTagRequest request, CancellationToken ct)
        => Success(await _tags.UpdateAsync(id, request, ct), "Tag updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _tags.DeleteAsync(id, ct);
        return SuccessMessage("Tag deleted.");
    }
}
