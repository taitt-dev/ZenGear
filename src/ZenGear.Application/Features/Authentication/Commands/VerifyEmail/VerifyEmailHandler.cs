using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;
using ZenGear.Domain.Enums;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.VerifyEmail;

/// <summary>
/// Handler for VerifyEmailCommand.
/// Validates OTP, confirms user email, and returns authentication tokens.
/// </summary>
public class VerifyEmailHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IEmailService emailService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<VerifyEmailCommand, Result<AuthenticationDto>>
{
    public async Task<Result<AuthenticationDto>> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        // Get user by email
        var userInfo = await identityService.GetByEmailAsync(request.Email, ct);

        if (userInfo == null)
        {
            return Result<AuthenticationDto>.Failure("User not found.", ErrorCodes.User.NotFound);
        }

        // Check if already verified
        if (userInfo.EmailConfirmed)
        {
            return Result<AuthenticationDto>.Failure("Email already verified.", ErrorCodes.User.EmailAlreadyVerified);
        }

        // Validate OTP
        var isValidOtp = await otpService.ValidateOtpAsync(
            userInfo.Id,
            request.OtpCode,
            OtpPurpose.EmailVerification,
            ct);

        if (!isValidOtp)
        {
            return Result<AuthenticationDto>.Failure(
                "Invalid or expired verification code.",
                ErrorCodes.User.InvalidOtpCode);
        }

        // Confirm email
        var confirmed = await identityService.ConfirmEmailAsync(userInfo.Id, ct);

        if (!confirmed)
        {
            return Result<AuthenticationDto>.Failure(
                "Failed to verify email.",
                ErrorCodes.User.EmailVerificationFailed);
        }

        // Send welcome email (don't fail if this fails)
        try
        {
            await emailService.SendWelcomeEmailAsync(
                userInfo.Email,
                userInfo.FirstName,
                ct);
        }
        catch
        {
            // Ignore welcome email failures
        }

        // Generate tokens (auto-login after verification)
        var accessToken = tokenService.GenerateAccessToken(
            userInfo.Id,
            userInfo.ExternalId,
            userInfo.Email,
            userInfo.FullName,
            [.. userInfo.Roles]);

        var refreshToken = tokenService.GenerateRefreshToken();

        // Store refresh token
        await refreshTokenRepository.CreateAsync(
            userInfo.Id,
            refreshToken,
            tokenService.GetRefreshTokenExpiration(),
            ct);

        // Get updated user info with roles
        var updatedUserInfo = await identityService.GetByIdAsync(userInfo.Id, ct);

        var authResponse = new AuthenticationDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = tokenService.GetAccessTokenExpiration(),
            User = new UserDto
            {
                Id = updatedUserInfo!.ExternalId,
                Email = updatedUserInfo.Email,
                FirstName = updatedUserInfo.FirstName,
                LastName = updatedUserInfo.LastName,
                FullName = updatedUserInfo.FullName,
                AvatarUrl = updatedUserInfo.AvatarUrl,
                Roles = [.. updatedUserInfo.Roles],
                EmailConfirmed = true
            }
        };

        return Result<AuthenticationDto>.Success(authResponse);
    }
}
