using CanvasArt.API.Models.DTOs.Paintings;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/home")]
public sealed class HomeController : ApiControllerBase
{
    private readonly ICmsService _cms;
    private readonly IPaintingService _paintings;
    private readonly ICategoryService _categories;

    public HomeController(ICmsService cms, IPaintingService paintings, ICategoryService categories)
    {
        _cms = cms;
        _paintings = paintings;
        _categories = categories;
    }

    /// <summary>Aggregated homepage payload: slideshow, featured paintings, categories and public settings.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var slides = await _cms.GetSlidesAsync(activeOnly: true, ct);
        var categories = await _categories.GetActiveAsync(ct);
        var featured = await _paintings.QueryAsync(
            new PaintingQuery { IsFeatured = true, PageSize = 12, SortBy = "featured", SortDir = "desc" },
            publishedOnly: true, ct);
        var settings = await _cms.GetSettingsAsync(null, ct);

        return Success(new
        {
            Slides = slides,
            Categories = categories,
            Featured = featured.Items,
            Settings = settings
        });
    }
}
