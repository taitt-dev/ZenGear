using Microsoft.EntityFrameworkCore;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Repositories;
using ZenGear.Infrastructure.Identity;

namespace ZenGear.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository for refresh token operations.
/// </summary>
public class RefreshTokenRepository : IRefreshTokenRepository
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    public RefreshTokenRepository(ApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public async Task<RefreshTokenInfo?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);

        if (refreshToken == null)
            return null;

        return new RefreshTokenInfo
        {
            Id = refreshToken.Id,
            UserId = refreshToken.UserId,
            Token = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresAt,
            CreatedAt = refreshToken.CreatedAt,
            RevokedAt = refreshToken.RevokedAt,
            ReplacedByToken = refreshToken.ReplacedByToken
        };
    }

    public async Task<string> CreateAsync(
        long userId,
        string token,
        DateTimeOffset expiresAt,
        CancellationToken ct = default)
    {
        var refreshToken = new RefreshToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedAt = _dateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync(ct);

        return token;
    }

    public async Task RevokeAsync(
        string token,
        string? replacedByToken = null,
        CancellationToken ct = default)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == token, ct);

        if (refreshToken == null)
            return;

        refreshToken.RevokedAt = _dateTime.UtcNow;
        refreshToken.ReplacedByToken = replacedByToken;

        await _context.SaveChangesAsync(ct);
    }

    public async Task RevokeAllForUserAsync(long userId, CancellationToken ct = default)
    {
        var tokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && rt.RevokedAt == null)
            .ToListAsync(ct);

        var now = _dateTime.UtcNow;

        foreach (var token in tokens)
        {
            token.RevokedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }
}
