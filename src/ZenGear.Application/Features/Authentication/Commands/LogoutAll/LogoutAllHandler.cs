using MediatR;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.LogoutAll;

/// <summary>
/// Handler for LogoutAllCommand.
/// Revokes all refresh tokens and updates security stamp.
/// </summary>
public class LogoutAllHandler(
    ICurrentUserService currentUser,
    IRefreshTokenRepository refreshTokenRepository,
    IIdentityService identityService)
    : IRequestHandler<LogoutAllCommand, Result>
{
    public async Task<Result> Handle(LogoutAllCommand request, CancellationToken ct)
    {
        var userId = currentUser.UserId;

        if (userId == 0)
        {
            return Result.Failure(
                "User not authenticated.",
                "UNAUTHORIZED");
        }

        // Revoke all refresh tokens for this user
        await refreshTokenRepository.RevokeAllForUserAsync(userId, ct);

        // Update security stamp to invalidate existing access tokens
        await identityService.UpdateSecurityStampAsync(userId, ct);

        return Result.Success();
    }
}
