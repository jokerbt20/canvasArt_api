using CanvasArt.API.Models.Common;
using CanvasArt.API.Models.DTOs.Auth;
using CanvasArt.API.Repository;
using CanvasArt.API.Services.Interfaces;
using CanvasArt.API.Models.Entities;

namespace CanvasArt.API.Services;

public sealed class AuthService : IAuthService
{
    private const string GuestRole = "Guest";

    private readonly IUserRepository _users;
    private readonly IRoleRepository _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwt;
    private readonly IDateTimeProvider _clock;

    public AuthService(
        IUserRepository users,
        IRoleRepository roles,
        IRefreshTokenRepository refreshTokens,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwt,
        IDateTimeProvider clock)
    {
        _users = users;
        _roles = roles;
        _refreshTokens = refreshTokens;
        _passwordHasher = passwordHasher;
        _jwt = jwt;
        _clock = clock;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(request.Email);
        if (await _users.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var role = await _roles.GetByNormalizedNameAsync(GuestRole.ToUpperInvariant(), cancellationToken)
                   ?? throw new NotFoundException("The default role is not configured.");

        var now = _clock.UtcNow;
        var user = new User
        {
            RoleId = role.Id,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.Id = await _users.CreateAsync(user, cancellationToken);
        user.RoleName = role.Name;

        return await IssueTokensAsync(user, role.Name, ip, cancellationToken);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByNormalizedEmailAsync(Normalize(request.Email), cancellationToken);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Invalid email or password.");

        if (!user.IsActive)
            throw new ForbiddenException("This account has been deactivated.");

        await _users.UpdateLastLoginAsync(user.Id, _clock.UtcNow, cancellationToken);
        return await IssueTokensAsync(user, user.RoleName ?? GuestRole, ip, cancellationToken);
    }

    public async Task<AuthResponse> RefreshAsync(RefreshTokenRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var identity = _jwt.ReadExpiredToken(request.AccessToken)
                       ?? throw new UnauthorizedException("Invalid access token.");

        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (stored is null || stored.UserId != identity.UserId || stored.JwtId != identity.JwtId)
            throw new UnauthorizedException("Invalid refresh token.");

        if (!stored.IsActive)
        {
            // Token reuse/expiry: revoke the whole chain defensively.
            await _refreshTokens.RevokeAllForUserAsync(stored.UserId, _clock.UtcNow, cancellationToken);
            throw new UnauthorizedException("Refresh token is no longer valid.");
        }

        var user = await _users.GetByIdAsync(stored.UserId, cancellationToken);
        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Account is unavailable.");

        var newRefresh = _jwt.CreateRefreshToken();
        await _refreshTokens.MarkUsedAndReplacedAsync(stored.Id, newRefresh, _clock.UtcNow, cancellationToken);

        return await IssueTokensAsync(user, user.RoleName ?? GuestRole, ip, cancellationToken, presetRefreshToken: newRefresh);
    }

    public async Task RevokeAsync(RevokeTokenRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _refreshTokens.GetByTokenAsync(request.RefreshToken, cancellationToken);
        if (stored is not null && stored.IsActive)
            await _refreshTokens.RevokeAsync(stored.Id, _clock.UtcNow, cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, string? ip, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = Normalize(request.Email);
        if (await _users.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new ConflictException("An account with this email already exists.");

        var role = await _roles.GetByNormalizedNameAsync(request.Role.ToUpperInvariant(), cancellationToken)
                   ?? throw new NotFoundException($"Role '{request.Role}' does not exist.");

        var now = _clock.UtcNow;
        var user = new User
        {
            RoleId = role.Id,
            Email = request.Email.Trim(),
            NormalizedEmail = normalizedEmail,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumber = request.PhoneNumber?.Trim(),
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now
        };
        user.Id = await _users.CreateAsync(user, cancellationToken);
        user.RoleName = role.Name;

        return ToDto(user);
    }

    public async Task<UserDto> GetCurrentAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User", userId);
        return ToDto(user);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken)
                   ?? throw new NotFoundException("User", userId);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException("The current password is incorrect.");

        var hash = _passwordHasher.Hash(request.NewPassword);
        await _users.UpdatePasswordAsync(userId, hash, _clock.UtcNow, cancellationToken);
        await _refreshTokens.RevokeAllForUserAsync(userId, _clock.UtcNow, cancellationToken);
    }

    public async Task<PagedResult<UserDto>> QueryUsersAsync(PagedQuery query, CancellationToken cancellationToken = default)
    {
        var page = await _users.QueryAsync(query, cancellationToken);
        var items = page.Items.Select(ToDto).ToList();
        return new PagedResult<UserDto>(items, page.TotalCount, page.Page, page.PageSize);
    }

    private async Task<AuthResponse> IssueTokensAsync(
        User user, string roleName, string? ip, CancellationToken cancellationToken, string? presetRefreshToken = null)
    {
        var access = _jwt.CreateAccessToken(user, roleName);
        var refresh = presetRefreshToken ?? _jwt.CreateRefreshToken();

        var now = _clock.UtcNow;
        await _refreshTokens.CreateAsync(new RefreshToken
        {
            UserId = user.Id,
            Token = refresh,
            JwtId = access.JwtId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(_jwt.RefreshTokenValidityDays),
            CreatedByIp = ip
        }, cancellationToken);

        return new AuthResponse
        {
            AccessToken = access.Token,
            RefreshToken = refresh,
            AccessTokenExpiresAt = access.ExpiresAtUtc,
            User = ToDto(user)
        };
    }

    private static string Normalize(string email) => email.Trim().ToUpperInvariant();

    private static UserDto ToDto(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        FirstName = u.FirstName,
        LastName = u.LastName,
        PhoneNumber = u.PhoneNumber,
        Role = u.RoleName ?? string.Empty,
        IsActive = u.IsActive,
        CreatedAt = u.CreatedAt,
        LastLoginAt = u.LastLoginAt
    };
}
