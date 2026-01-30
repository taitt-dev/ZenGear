using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Enums;

namespace ZenGear.Application.Features.Authentication.Commands.ForgotPassword;

/// <summary>
/// Handler for ForgotPasswordCommand.
/// Generates password reset OTP and sends email.
/// </summary>
public class ForgotPasswordHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IEmailService emailService)
    : IRequestHandler<ForgotPasswordCommand, Result>
{
    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        // Get user (but don't reveal if email doesn't exist for security)
        var userInfo = await identityService.GetByEmailAsync(request.Email, ct);

        if (userInfo == null)
        {
            // Return success to prevent email enumeration
            return Result.Success();
        }

        // Check rate limit
        var isRateLimited = await otpService.IsRateLimitExceededAsync(
            userInfo.Id,
            OtpPurpose.PasswordReset,
            ct);

        if (isRateLimited)
        {
            return Result.Failure(
                "Too many requests. Please try again later.",
                ErrorCodes.User.OtpRateLimitExceeded);
        }

        // Generate OTP
        var otpCode = await otpService.CreateOtpAsync(
            userInfo.Id,
            OtpPurpose.PasswordReset,
            ct);

        // Send email
        try
        {
            await emailService.SendPasswordResetAsync(
                userInfo.Email,
                userInfo.FirstName,
                otpCode,
                ct);
        }
        catch
        {
            return Result.Failure(
                "Failed to send password reset email.",
                ErrorCodes.User.EmailSendFailed);
        }

        return Result.Success();
    }
}
