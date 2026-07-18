using CanvasArt.API.Authorization;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Cms;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/testimonials")]
public sealed class TestimonialsController : ApiControllerBase
{
    private readonly ICmsService _cms;

    public TestimonialsController(ICmsService cms) => _cms = cms;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Success(await _cms.GetTestimonialsAsync(activeOnly: true, ct));

    [HttpGet("manage")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
        => Success(await _cms.GetTestimonialsAsync(activeOnly: false, ct));

    [HttpGet("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _cms.GetTestimonialAsync(id, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Create([FromForm] CreateTestimonialRequest request, IFormFile? file, CancellationToken ct)
    {
        if (file is not null && file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            return Created(await _cms.CreateTestimonialAsync(request, stream, file.FileName, ct), "Testimonial created.");
        }
        return Created(await _cms.CreateTestimonialAsync(request, null, null, ct), "Testimonial created.");
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    [RequestSizeLimit(15 * 1024 * 1024)]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateTestimonialRequest request, IFormFile? file, CancellationToken ct)
    {
        if (file is not null && file.Length > 0)
        {
            await using var stream = file.OpenReadStream();
            return Success(await _cms.UpdateTestimonialAsync(id, request, stream, file.FileName, ct), "Testimonial updated.");
        }
        return Success(await _cms.UpdateTestimonialAsync(id, request, null, null, ct), "Testimonial updated.");
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _cms.DeleteTestimonialAsync(id, ct);
        return SuccessMessage("Testimonial deleted.");
    }
}
