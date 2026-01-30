using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;

namespace ZenGear.Application.Features.Authentication.Commands.Login;

/// <summary>
/// Command to login user with email and password.
/// Returns access token and refresh token.
/// </summary>
public record LoginCommand : IRequest<Result<AuthenticationDto>>
{
    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("password")]
    public required string Password { get; init; }
}
