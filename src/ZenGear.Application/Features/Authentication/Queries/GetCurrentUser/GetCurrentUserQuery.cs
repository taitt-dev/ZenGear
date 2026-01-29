using MediatR;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;

namespace ZenGear.Application.Features.Authentication.Queries.GetCurrentUser;

/// <summary>
/// Query to get current authenticated user information.
/// </summary>
public record GetCurrentUserQuery : IRequest<Result<UserDto>>;
