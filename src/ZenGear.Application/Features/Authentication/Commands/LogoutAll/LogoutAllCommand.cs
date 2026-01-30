using MediatR;
using ZenGear.Application.Common.Models;

namespace ZenGear.Application.Features.Authentication.Commands.LogoutAll;

/// <summary>
/// Command to logout from all devices.
/// Revokes all refresh tokens and updates security stamp to invalidate existing tokens.
/// </summary>
public record LogoutAllCommand : IRequest<Result>
{
    // No parameters needed - user ID will be extracted from JWT claims
}
