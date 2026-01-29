using MediatR;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Repositories;

namespace ZenGear.Application.Features.Authentication.Commands.Logout;

/// <summary>
/// Handler for LogoutCommand.
/// Revokes the refresh token.
/// </summary>
public class LogoutHandler(IRefreshTokenRepository refreshTokenRepository)
    : IRequestHandler<LogoutCommand, Result>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken ct)
    {
        // Revoke token (no error if token not found)
        await refreshTokenRepository.RevokeAsync(request.RefreshToken, null, ct);

        return Result.Success();
    }
}
