using System.Security.Claims;
using CanvasArt.API.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public abstract class ApiControllerBase : ControllerBase
{
    protected int? CurrentUserId
    {
        get
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            return int.TryParse(value, out var id) ? id : null;
        }
    }

    protected string? RemoteIp => HttpContext.Connection.RemoteIpAddress?.ToString();

    protected IActionResult Success<T>(T data, string message = "Request succeeded.") =>
        Ok(ApiResponse<T>.Ok(data, message));

    protected IActionResult Created<T>(T data, string message = "Resource created.") =>
        StatusCode(StatusCodes.Status201Created, ApiResponse<T>.Ok(data, message));

    protected IActionResult SuccessMessage(string message = "Request succeeded.") =>
        Ok(ApiResponse.Ok(message));
}
