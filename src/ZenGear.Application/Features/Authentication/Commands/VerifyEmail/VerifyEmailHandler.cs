using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Enums;

namespace ZenGear.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Handler for VerifyEmailCommand.
/// Validates OTP and confirms user email.
/// </summary>
public class VerifyEmailHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IEmailService emailService)
    : IRequestHandler<VerifyEmailCommand, Result>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        // Get user by email
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

        // Validate OTP
        var isValidOtp = await otpService.ValidateOtpAsync(
            userInfo.Id,
            request.OtpCode,
            OtpPurpose.EmailVerification,
            ct);

        if (!isValidOtp)
        {
            return Result.Failure(
                "Invalid or expired verification code.",
                ErrorCodes.User.InvalidOtpCode);
        }

        // Confirm email
        var confirmed = await identityService.ConfirmEmailAsync(userInfo.Id, ct);

        if (!confirmed)
        {
            return Result.Failure(
                "Failed to verify email.",
                ErrorCodes.User.EmailVerificationFailed);
        }

        // Send welcome email
        try
        {
            await emailService.SendWelcomeEmailAsync(
                userInfo.Email,
                userInfo.FirstName,
                ct);
        }
        catch
        {
            // Don't fail if welcome email fails
        }

        return Result.Success();
    }
}
