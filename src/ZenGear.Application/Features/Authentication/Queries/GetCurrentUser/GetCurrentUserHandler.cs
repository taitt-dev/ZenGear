using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;

namespace ZenGear.Application.Features.Authentication.Queries.GetCurrentUser;

/// <summary>
/// Handler for GetCurrentUserQuery.
/// Returns current authenticated user information.
/// </summary>
public class GetCurrentUserHandler(
    IIdentityService identityService,
    ICurrentUserService currentUser)
    : IRequestHandler<GetCurrentUserQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(GetCurrentUserQuery request, CancellationToken ct)
    {
        if (!currentUser.IsAuthenticated)
        {
            return Result<UserDto>.Failure("User not authenticated.", ErrorCodes.Unauthorized);
        }

        var userInfo = await identityService.GetByIdAsync(currentUser.UserId, ct);

        if (userInfo == null)
        {
            return Result<UserDto>.Failure("User not found.", ErrorCodes.User.NotFound);
        }

        var userDto = new UserDto
        {
            Id = userInfo.ExternalId,
            Email = userInfo.Email,
            FirstName = userInfo.FirstName,
            LastName = userInfo.LastName,
            FullName = userInfo.FullName,
            AvatarUrl = userInfo.AvatarUrl,
            Roles = userInfo.Roles,
            EmailConfirmed = userInfo.EmailConfirmed
        };

        return Result<UserDto>.Success(userDto);
    }
}
