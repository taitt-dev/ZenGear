using System.Text.Json.Serialization;
using MediatR;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.DTOs;

namespace ZenGear.Application.Features.Authentication.Commands.RefreshToken;

/// <summary>
/// Command to refresh access token using refresh token.
/// Implements token rotation for security.
/// </summary>
public record RefreshTokenCommand : IRequest<Result<RefreshTokenDto>>
{
    [JsonPropertyName("refreshToken")]
    public required string RefreshToken { get; init; }
}
