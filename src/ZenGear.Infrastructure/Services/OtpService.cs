using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Domain.Enums;
using ZenGear.Infrastructure.Identity;
using ZenGear.Infrastructure.Persistence;

namespace ZenGear.Infrastructure.Services;

/// <summary>
/// Service for generating and validating email OTPs.
/// Implements rate limiting and single-use tokens.
/// </summary>
public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _context;
    private readonly IDateTime _dateTime;

    // Rate limiting: max 5 OTP requests per 15 minutes
    private const int MaxOtpRequestsPerWindow = 5;
    private const int RateLimitWindowMinutes = 15;

    // OTP validity: 10 minutes
    private const int OtpValidityMinutes = 10;

    public OtpService(ApplicationDbContext context, IDateTime dateTime)
    {
        _context = context;
        _dateTime = dateTime;
    }

    public string GenerateOtpCode()
    {
        var number = RandomNumberGenerator.GetInt32(100000, 999999);
        return number.ToString("D6");
    }

    public async Task<string> CreateOtpAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var code = GenerateOtpCode();
        var now = _dateTime.UtcNow;

        var emailOtp = new EmailOtp
        {
            UserId = userId,
            Code = code,
            Purpose = purpose,
            ExpiresAt = now.AddMinutes(OtpValidityMinutes),
            CreatedAt = now,
            IsUsed = false
        };

        _context.EmailOtps.Add(emailOtp);
        await _context.SaveChangesAsync(ct);

        return code;
    }

    public async Task<bool> ValidateOtpAsync(
        long userId,
        string code,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var now = _dateTime.UtcNow;

        var otp = await _context.EmailOtps
            .FirstOrDefaultAsync(
                o => o.UserId == userId &&
                     o.Code == code &&
                     o.Purpose == purpose &&
                     !o.IsUsed &&
                     o.ExpiresAt > now,
                ct);

        if (otp == null)
            return false;

        otp.IsUsed = true;
        await _context.SaveChangesAsync(ct);

        return true;
    }

    public async Task InvalidateOtpsAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var otps = await _context.EmailOtps
            .Where(otp => otp.UserId == userId &&
                          otp.Purpose == purpose &&
                          !otp.IsUsed)
            .ToListAsync(ct);

        foreach (var otp in otps)
        {
            otp.IsUsed = true;
        }

        await _context.SaveChangesAsync(ct);
    }

    public async Task<bool> IsRateLimitExceededAsync(
        long userId,
        OtpPurpose purpose,
        CancellationToken ct = default)
    {
        var now = _dateTime.UtcNow;
        var rateLimitStart = now.AddMinutes(-RateLimitWindowMinutes);

        var recentOtpCount = await _context.EmailOtps
            .CountAsync(
                otp => otp.UserId == userId &&
                       otp.Purpose == purpose &&
                       otp.CreatedAt >= rateLimitStart,
                ct);

        return recentOtpCount >= MaxOtpRequestsPerWindow;
    }
}
