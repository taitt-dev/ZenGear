using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Handler for LoginCommand.
/// Validates credentials and generates tokens.
/// </summary>
public class LoginHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<LoginCommand, Result<AuthenticationDto>>
{
    public async Task<Result<AuthenticationDto>> Handle(LoginCommand request, CancellationToken ct)
    {
        // Get user
        var userInfo = await identityService.GetByEmailAsync(request.Email, ct);

        if (userInfo == null)
        {
            return Result<AuthenticationDto>.Failure(
                "Invalid email or password.",
                ErrorCodes.User.InvalidCredentials);
        }

        // Check if email is verified
        if (!userInfo.EmailConfirmed)
        {
            return Result<AuthenticationDto>.Failure(
                "Email not verified. Please verify your email first.",
                ErrorCodes.User.EmailNotVerified);
        }

        // Check if account is locked out
        var isLockedOut = await identityService.IsLockedOutAsync(userInfo.Id, ct);
        if (isLockedOut)
        {
            var lockoutEnd = await identityService.GetLockoutEndAsync(userInfo.Id, ct);
            var remainingMinutes = lockoutEnd.HasValue
                ? Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes)
                : 0;

            var message = remainingMinutes > 0
                ? $"Account is locked due to too many failed login attempts. Please try again in {remainingMinutes} minute(s)."
                : "Account is locked due to too many failed login attempts. Please try again later.";

            return Result<AuthenticationDto>.Failure(
                message,
                ErrorCodes.User.AccountLocked);
        }

        // Check password
        var isValidPassword = await identityService.CheckPasswordAsync(
            request.Email,
            request.Password,
            ct);

        if (!isValidPassword)
        {
            // Increment failed login count
            var shouldLockout = await identityService.IncrementAccessFailedCountAsync(userInfo.Id, ct);

            if (shouldLockout)
            {
                return Result<AuthenticationDto>.Failure(
                    "Account locked due to too many failed login attempts.",
                    ErrorCodes.User.AccountLocked);
            }

            return Result<AuthenticationDto>.Failure(
                "Invalid email or password.",
                ErrorCodes.User.InvalidCredentials);
        }

        // Reset failed login count
        await identityService.ResetAccessFailedCountAsync(userInfo.Id, ct);

        // Generate tokens
        var accessToken = tokenService.GenerateAccessToken(
            userInfo.Id,
            userInfo.ExternalId,
            userInfo.Email,
            userInfo.FullName,
            userInfo.Roles);

        var refreshToken = tokenService.GenerateRefreshToken();

        // Store refresh token
        await refreshTokenRepository.CreateAsync(
            userInfo.Id,
            refreshToken,
            tokenService.GetRefreshTokenExpiration(),
            ct);

        // Build response
        var response = new AuthenticationDto
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = tokenService.GetAccessTokenExpiration(),
            User = new UserDto
            {
                Id = userInfo.ExternalId,
                Email = userInfo.Email,
                FirstName = userInfo.FirstName,
                LastName = userInfo.LastName,
                FullName = userInfo.FullName,
                AvatarUrl = userInfo.AvatarUrl,
                Roles = userInfo.Roles,
                EmailConfirmed = userInfo.EmailConfirmed
            }
        };

        return Result<AuthenticationDto>.Success(response);
    }
}
