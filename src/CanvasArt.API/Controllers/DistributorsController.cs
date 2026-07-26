using CanvasArt.API.Authorization;
using CanvasArt.API.Models.DTOs.Distributors;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/distributors")]
[Authorize(Roles = RoleNames.Administrator)]
public sealed class DistributorsController : ApiControllerBase
{
    private readonly IDistributorService _distributors;

    public DistributorsController(IDistributorService distributors) => _distributors = distributors;

    [HttpGet]
    public async Task<IActionResult> Query([FromQuery] DistributorQuery query, CancellationToken ct)
        => Success(await _distributors.QueryAsync(query, ct));

    /// <summary>Sales leaderboard: distributors ranked by total attributed sales.</summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard([FromQuery] DistributorDashboardQuery query, CancellationToken ct)
        => Success(await _distributors.GetDashboardAsync(query, ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
        => Success(await _distributors.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreateDistributorRequest request, CancellationToken ct)
        => Created(await _distributors.CreateAsync(request, ct), "Distributor created.");

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, UpdateDistributorRequest request, CancellationToken ct)
        => Success(await _distributors.UpdateAsync(id, request, ct), "Distributor updated.");

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _distributors.DeleteAsync(id, ct);
        return SuccessMessage("Distributor deleted.");
    }

    [HttpGet("{id:int}/promo-codes")]
    public async Task<IActionResult> GetPromoCodes(int id, CancellationToken ct)
        => Success(await _distributors.GetPromoCodesAsync(id, ct));
}
