using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/frame-previews")]
public sealed class FramePreviewsController : ApiControllerBase
{
    private readonly IFramePreviewService _previews;

    public FramePreviewsController(IFramePreviewService previews) => _previews = previews;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get([FromQuery] int paintingId, [FromQuery] int frameId, [FromQuery] int? paintingImageId, CancellationToken ct)
        => Success(await _previews.GetOrBuildAsync(paintingId, frameId, paintingImageId, ct));
}
