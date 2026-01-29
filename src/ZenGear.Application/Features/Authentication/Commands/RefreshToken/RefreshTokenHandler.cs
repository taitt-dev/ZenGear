using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Handler for RefreshTokenCommand.
/// Validates refresh token and generates new token pair.
/// Implements token rotation (old token revoked, new token issued).
/// </summary>
public class RefreshTokenHandler(
    IIdentityService identityService,
    ITokenService tokenService,
    IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenDto>>
{
    public async Task<Result<RefreshTokenDto>> Handle(
        RefreshTokenCommand request,
        CancellationToken ct)
    {
        // Get refresh token
        var refreshTokenInfo = await refreshTokenRepository.GetByTokenAsync(request.RefreshToken, ct);

        if (refreshTokenInfo == null)
        {
            return Result<RefreshTokenDto>.Failure(
                "Invalid refresh token.",
                ErrorCodes.User.InvalidRefreshToken);
        }

        // Check if token is active (not revoked and not expired)
        if (!refreshTokenInfo.IsActive)
        {
            return Result<RefreshTokenDto>.Failure(
                "Refresh token is no longer valid.",
                ErrorCodes.User.RefreshTokenExpired);
        }

        // Get user info
        var userInfo = await identityService.GetByIdAsync(refreshTokenInfo.UserId, ct);

        if (userInfo == null)
        {
            return Result<RefreshTokenDto>.Failure(
                "User not found.",
                ErrorCodes.User.NotFound);
        }

        // Generate new token pair
        var newAccessToken = tokenService.GenerateAccessToken(
            userInfo.Id,
            userInfo.ExternalId,
            userInfo.Email,
            userInfo.FullName,
            userInfo.Roles);

        var newRefreshToken = tokenService.GenerateRefreshToken();

        // Store new refresh token
        await refreshTokenRepository.CreateAsync(
            userInfo.Id,
            newRefreshToken,
            tokenService.GetRefreshTokenExpiration(),
            ct);

        // Revoke old refresh token (token rotation)
        await refreshTokenRepository.RevokeAsync(
            request.RefreshToken,
            newRefreshToken,
            ct);

        // Build response
        var response = new RefreshTokenDto
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = tokenService.GetAccessTokenExpiration()
        };

        return Result<RefreshTokenDto>.Success(response);
    }
}
