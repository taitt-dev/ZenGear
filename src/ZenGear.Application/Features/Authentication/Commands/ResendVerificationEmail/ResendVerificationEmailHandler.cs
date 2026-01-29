using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Enums;

namespace ZenGear.Application.Features.Authentication.Commands.ResendVerificationEmail;

/// <summary>
/// Handler for ResendVerificationEmailCommand.
/// Generates new OTP and sends email (with rate limiting).
/// </summary>
public class ResendVerificationEmailHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IEmailService emailService)
    : IRequestHandler<ResendVerificationEmailCommand, Result>
{
    public async Task<Result> Handle(ResendVerificationEmailCommand request, CancellationToken ct)
    {
        // Get user
        var userInfo = await identityService.GetByEmailAsync(request.Email, ct);

        if (userInfo == null)
        {
            return Result.Failure("User not found.", ErrorCodes.User.NotFound);
        }

        // Check if already verified
        if (userInfo.EmailConfirmed)
        {
            return Result.Failure("Email already verified.", ErrorCodes.User.EmailAlreadyVerified);
        }

        // Check rate limit
        var isRateLimited = await otpService.IsRateLimitExceededAsync(
            userInfo.Id,
            OtpPurpose.EmailVerification,
            ct);

        if (isRateLimited)
        {
            return Result.Failure(
                "Too many requests. Please try again later.",
                ErrorCodes.User.OtpRateLimitExceeded);
        }

        // Generate new OTP
        var otpCode = await otpService.CreateOtpAsync(
            userInfo.Id,
            OtpPurpose.EmailVerification,
            ct);

        // Send email
        await emailService.SendEmailVerificationAsync(
            userInfo.Email,
            userInfo.FirstName,
            otpCode,
            ct);

        return Result.Success();
    }
}
