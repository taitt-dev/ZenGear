using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ZenGear.Application.Common.Interfaces;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// Service for accessing current authenticated user information from JWT claims.
/// </summary>
public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Internal user ID (long) from "nameid" claim.
    /// Returns 0 if not authenticated.
    /// </summary>
    public long UserId
    {
        get
        {
            var userIdClaim = _httpContextAccessor.HttpContext?.User?
                .FindFirst(ClaimTypes.NameIdentifier)?.Value;

            return long.TryParse(userIdClaim, out var userId) ? userId : 0;
        }
    }

    /// <summary>
    /// External user ID (string) from "sub" claim.
    /// Format: usr_xxx
    /// Returns null if not authenticated.
    /// </summary>
    public string? UserExternalId => _httpContextAccessor.HttpContext?.User?
        .FindFirst("sub")?.Value;

    /// <summary>
    /// User email from "email" claim.
    /// Returns null if not authenticated.
    /// </summary>
    public string? Email => _httpContextAccessor.HttpContext?.User?
        .FindFirst(ClaimTypes.Email)?.Value;

    /// <summary>
    /// Whether the current user is authenticated.
    /// </summary>
    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    /// <summary>
    /// User roles from "role" claims.
    /// Returns empty array if not authenticated.
    /// </summary>
    public string[] Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null)
                return [];

            return user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToArray();
        }
    }
}
