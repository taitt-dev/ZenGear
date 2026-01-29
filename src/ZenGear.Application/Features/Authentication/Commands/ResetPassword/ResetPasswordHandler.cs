using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Enums;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.ResetPassword;

/// <summary>
/// Handler for ResetPasswordCommand.
/// Validates OTP and resets password.
/// </summary>
public class ResetPasswordHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<ResetPasswordCommand, Result>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        // Get user
        var userInfo = await identityService.GetByEmailAsync(request.Email, ct);

        if (userInfo == null)
        {
            return Result.Failure(
                "Invalid verification code.",
                ErrorCodes.User.InvalidOtpCode);
        }

        // Validate OTP
        var isValidOtp = await otpService.ValidateOtpAsync(
            userInfo.Id,
            request.OtpCode,
            OtpPurpose.PasswordReset,
            ct);

        if (!isValidOtp)
        {
            return Result.Failure(
                "Invalid or expired verification code.",
                ErrorCodes.User.InvalidOtpCode);
        }

        // Reset password
        var (succeeded, errors) = await identityService.ResetPasswordAsync(
            userInfo.Id,
            request.NewPassword,
            ct);

        if (!succeeded)
        {
            return Result.Failure(errors, ErrorCodes.User.PasswordResetFailed);
        }

        // Update security stamp (invalidates all tokens)
        await identityService.UpdateSecurityStampAsync(userInfo.Id, ct);

        // Revoke all refresh tokens (logout from all devices)
        await refreshTokenRepository.RevokeAllForUserAsync(userInfo.Id, ct);

        return Result.Success();
    }
}
