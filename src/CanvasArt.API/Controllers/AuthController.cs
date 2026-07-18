using CanvasArt.API.Authorization;
using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Auth;
using CanvasArt.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CanvasArt.API.Controllers;

[Route("api/auth")]
public sealed class AuthController : ApiControllerBase
{
    private readonly IAuthService _auth;

    public AuthController(IAuthService auth) => _auth = auth;

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
        => Created(await _auth.RegisterAsync(request, RemoteIp, ct), "Registration successful.");

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
        => Success(await _auth.LoginAsync(request, RemoteIp, ct), "Login successful.");

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request, CancellationToken ct)
        => Success(await _auth.RefreshAsync(request, RemoteIp, ct), "Token refreshed.");

    [HttpPost("revoke")]
    [AllowAnonymous]
    public async Task<IActionResult> Revoke(RevokeTokenRequest request, CancellationToken ct)
    {
        await _auth.RevokeAsync(request, ct);
        return SuccessMessage("Token revoked.");
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken ct)
        => Success(await _auth.GetCurrentAsync(CurrentUserId!.Value, ct));

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken ct)
    {
        await _auth.ChangePasswordAsync(CurrentUserId!.Value, request, ct);
        return SuccessMessage("Password changed. Please sign in again.");
    }

    [HttpPost("users")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> CreateUser(CreateUserRequest request, CancellationToken ct)
        => Created(await _auth.CreateUserAsync(request, RemoteIp, ct), "User created.");

    [HttpGet("users")]
    [Authorize(Roles = RoleNames.Administrator)]
    public async Task<IActionResult> ListUsers([FromQuery] PagedUserQuery query, CancellationToken ct)
        => Success(await _auth.QueryUsersAsync(query, ct));
}

/// <summary>Concrete <see cref="PagedQuery"/> so it can be model-bound from the query string.</summary>
public sealed class PagedUserQuery : PagedQuery;
