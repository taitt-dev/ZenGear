using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.ChangePassword;

/// <summary>
/// Handler for ChangePasswordCommand.
/// Changes password for currently authenticated user.
/// </summary>
public class ChangePasswordHandler(
    IIdentityService identityService,
    ICurrentUserService currentUser)
    : IRequestHandler<ChangePasswordCommand, Result>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result.Failure("User not authenticated.", ErrorCodes.Unauthorized);
        }

        var (succeeded, errors) = await identityService.ChangePasswordAsync(
            currentUser.UserId,
            request.CurrentPassword,
            request.NewPassword,
            ct);

        if (!succeeded)
        {
            return Result.Failure(errors, ErrorCodes.User.PasswordChangeFailed);
        }

        // Update security stamp (invalidates existing tokens)
        await identityService.UpdateSecurityStampAsync(currentUser.UserId, ct);

        return Result.Success();
    }
}
