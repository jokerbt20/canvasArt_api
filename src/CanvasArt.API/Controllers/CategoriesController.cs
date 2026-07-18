using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Categories;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/categories")]
public sealed class CategoriesController : ApiControllerBase
{
    private readonly ICategoryService _categories;

    public CategoriesController(ICategoryService categories) => _categories = categories;

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive(CancellationToken ct)
        => Success(await _categories.GetActiveAsync(ct));

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _categories.GetByIdAsync(id, ct));

    [HttpGet("manage")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Query([FromQuery] CategoryQuery query, CancellationToken ct)
        => Success(await _categories.QueryAsync(query, ct));

    [HttpPost]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken ct)
        => Created(await _categories.CreateAsync(request, ct), "Category created.");

    [HttpPut("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Update(int id, UpdateCategoryRequest request, CancellationToken ct)
        => Success(await _categories.UpdateAsync(id, request, ct), "Category updated.");

    [HttpDelete("{id:int}")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _categories.DeleteAsync(id, ct);
        return SuccessMessage("Category deleted.");
    }
}
