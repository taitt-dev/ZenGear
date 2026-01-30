using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenGear.Application.Common.Models;
using ZenGear.Application.Features.Authentication.Commands.ChangePassword;
using ZenGear.Application.Features.Authentication.Commands.ForgotPassword;
using ZenGear.Application.Features.Authentication.Commands.Login;
using ZenGear.Application.Features.Authentication.Commands.Logout;
using ZenGear.Application.Features.Authentication.Commands.LogoutAll;
using ZenGear.Application.Features.Authentication.Commands.RefreshToken;
using ZenGear.Application.Features.Authentication.Commands.Register;
using ZenGear.Application.Features.Authentication.Commands.ResendVerificationEmail;
using ZenGear.Application.Features.Authentication.Commands.ResetPassword;
using ZenGear.Application.Features.Authentication.Commands.VerifyEmail;
using ZenGear.Application.Features.Authentication.DTOs;
using ZenGear.Application.Features.Authentication.Queries.GetCurrentUser;

namespace ZenGear.Api.Controllers.V1;

/// <summary>
/// Authentication endpoints for user registration, login, and password management.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthenticationController(ISender sender) : ControllerBase
{
    /// <summary>
    /// Register a new user account.
    /// Email verification OTP will be sent automatically.
    /// </summary>
    /// <param name="command">Registration details</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">User registered successfully. Check email for verification code.</response>
    /// <response code="400">Validation error (e.g., email already exists)</response>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Verify email with OTP code.
    /// Required before first login.
    /// </summary>
    /// <param name="command">Email and OTP code</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or expired OTP code</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Resend email verification OTP.
    /// Rate limited to prevent abuse (5 requests per 15 minutes).
    /// </summary>
    /// <param name="command">Email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Verification code sent successfully</response>
    /// <response code="400">Rate limit exceeded or email already verified</response>
    [HttpPost("resend-verification-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResendVerificationEmail(
        [FromBody] ResendVerificationEmailCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Login with email and password.
    /// Returns access token and refresh token.
    /// Email must be verified before login.
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Login successful with tokens</response>
    /// <response code="400">Invalid credentials or email not verified</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<AuthenticationDto>.Success(result.Data!));
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// Implements token rotation (old token revoked, new tokens issued).
    /// </summary>
    /// <param name="command">Refresh token</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">New tokens issued successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(
        [FromBody] RefreshTokenCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<RefreshTokenDto>.Success(result.Data!));
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// Client should also discard access token locally.
    /// </summary>
    /// <param name="command">Refresh token to revoke</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Logged out successfully</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Logout from all devices.
    /// Revokes all refresh tokens and updates security stamp to invalidate all existing access tokens.
    /// User must re-authenticate on all devices.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Logged out from all devices successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("logout-all")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var result = await sender.Send(new LogoutAllCommand(), ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return Unauthorized(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Change password for authenticated user.
    /// Requires current password.
    /// Invalidates all existing tokens (must login again).
    /// </summary>
    /// <param name="command">Current and new password</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Password changed successfully</response>
    /// <response code="400">Invalid current password or weak new password</response>
    /// <response code="401">User not authenticated</response>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Request password reset OTP.
    /// Sends OTP to user's email if account exists.
    /// </summary>
    /// <param name="command">Email address</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Password reset code sent (or email not found - no disclosure)</response>
    /// <response code="400">Rate limit exceeded</response>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword(
        [FromBody] ForgotPasswordCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Reset password using OTP.
    /// Invalidates all existing tokens (must login again).
    /// </summary>
    /// <param name="command">Email, OTP code, and new password</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Password reset successfully</response>
    /// <response code="400">Invalid or expired OTP</response>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword(
        [FromBody] ResetPasswordCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse.Success());
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Get current authenticated user information.
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">User information retrieved successfully</response>
    /// <response code="401">User not authenticated</response>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken ct)
    {
        var result = await sender.Send(new GetCurrentUserQuery(), ct);

        if (result.Succeeded)
        {
            return Ok(ApiResponse<UserDto>.Success(result.Data!));
        }

        return Unauthorized(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }
}
