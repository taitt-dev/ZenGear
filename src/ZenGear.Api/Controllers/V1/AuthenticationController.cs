using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ZenGear.Application.Common.Constants;
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
/// Supports hybrid token storage: httpOnly cookies for web browsers, JSON body for mobile apps.
/// </summary>
[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ApiVersion("1.0")]
[Produces("application/json")]
public class AuthenticationController(ISender sender) : ControllerBase
{
    private const string RefreshTokenCookieName = "refreshToken";
    private const int RefreshTokenExpirationDays = 7;

    /// <summary>
    /// Detect if the request is from a web browser or mobile app based on User-Agent.
    /// </summary>
    /// <returns>True if web browser, false if mobile app</returns>
    private bool IsWebBrowser()
    {
        var userAgent = Request.Headers.UserAgent.ToString();

        // Identify mobile apps
        if (userAgent.Contains("Flutter", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("Dart", StringComparison.OrdinalIgnoreCase) ||
            userAgent.Contains("okhttp", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Identify web browsers
        return userAgent.Contains("Mozilla", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase) ||
               userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Set refresh token as httpOnly cookie for web browsers.
    /// </summary>
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // HTTPS only
            SameSite = SameSiteMode.Strict,
            Path = "/api/v1/auth/refresh-token",
            Expires = DateTimeOffset.UtcNow.AddDays(RefreshTokenExpirationDays)
        };

        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, cookieOptions);
    }

    /// <summary>
    /// Clear refresh token cookie.
    /// </summary>
    private void ClearRefreshTokenCookie()
    {
        Response.Cookies.Delete(RefreshTokenCookieName);
    }
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
    /// <summary>
    /// Verify email with OTP code.
    /// For web browsers: Returns access token and user info. Refresh token sent via httpOnly cookie.
    /// For mobile apps: Returns both access token and refresh token in JSON body.
    /// Required before first login.
    /// </summary>
    /// <param name="command">Email and OTP code</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Email verified successfully</response>
    /// <response code="400">Invalid or expired OTP code</response>
    [HttpPost("verify-email")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WebAuthenticationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> VerifyEmail(
        [FromBody] VerifyEmailCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            var authData = result.Data!;
            var isWebBrowser = IsWebBrowser();

            if (isWebBrowser)
            {
                // Web: Set refresh token in httpOnly cookie
                SetRefreshTokenCookie(authData.RefreshToken);

                // Return only access token and user info (no refresh token in JSON)
                var webResponse = new WebAuthenticationDto
                {
                    AccessToken = authData.AccessToken,
                    ExpiresAt = authData.ExpiresAt,
                    User = authData.User
                };

                return Ok(ApiResponse<WebAuthenticationDto>.Success(webResponse));
            }

            // Mobile: Return both tokens in JSON body
            return Ok(ApiResponse<AuthenticationDto>.Success(authData));
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
    /// For web browsers: Returns access token and user info. Refresh token sent via httpOnly cookie.
    /// For mobile apps: Returns both access token and refresh token in JSON body.
    /// Email must be verified before login.
    /// </summary>
    /// <param name="command">Login credentials</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Login successful with tokens</response>
    /// <response code="400">Invalid credentials or email not verified</response>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthenticationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WebAuthenticationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromBody] LoginCommand command,
        CancellationToken ct)
    {
        var result = await sender.Send(command, ct);

        if (result.Succeeded)
        {
            var authData = result.Data!;
            var isWebBrowser = IsWebBrowser();

            if (isWebBrowser)
            {
                // Web: Set refresh token in httpOnly cookie
                SetRefreshTokenCookie(authData.RefreshToken);

                // Return only access token and user info (no refresh token in JSON)
                var webResponse = new WebAuthenticationDto
                {
                    AccessToken = authData.AccessToken,
                    ExpiresAt = authData.ExpiresAt,
                    User = authData.User
                };

                return Ok(ApiResponse<WebAuthenticationDto>.Success(webResponse));
            }

            // Mobile: Return both tokens in JSON body
            return Ok(ApiResponse<AuthenticationDto>.Success(authData));
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Refresh access token using refresh token.
    /// For web browsers: Reads refresh token from httpOnly cookie, returns new access token. New refresh token sent via cookie.
    /// For mobile apps: Reads refresh token from request body, returns both new tokens in JSON body.
    /// Implements token rotation (old token revoked, new tokens issued).
    /// </summary>
    /// <param name="command">Refresh token (optional, will try cookie first for web browsers)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">New tokens issued successfully</response>
    /// <response code="400">Invalid or expired refresh token</response>
    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RefreshTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WebRefreshTokenDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshToken(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] RefreshTokenCommand? command,
        CancellationToken ct)
    {
        string? refreshToken = null;

        // Try cookie first (web browsers)
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken))
        {
            refreshToken = cookieToken;
        }
        // Fallback to request body (mobile apps)
        else if (command?.RefreshToken is not null)
        {
            refreshToken = command.RefreshToken;
        }

        if (string.IsNullOrEmpty(refreshToken))
        {
            return BadRequest(ApiResponse.Failure(
                "Refresh token is required.",
                ErrorCodes.Auth.InvalidToken));
        }

        var refreshCommand = new RefreshTokenCommand { RefreshToken = refreshToken };
        var result = await sender.Send(refreshCommand, ct);

        if (result.Succeeded)
        {
            var newTokens = result.Data!;
            var hasCookie = Request.Cookies.ContainsKey(RefreshTokenCookieName);

            if (hasCookie)
            {
                // Web: Update refresh token cookie, return only access token
                SetRefreshTokenCookie(newTokens.RefreshToken);

                var webResponse = new WebRefreshTokenDto
                {
                    AccessToken = newTokens.AccessToken,
                    ExpiresAt = newTokens.ExpiresAt
                };

                return Ok(ApiResponse<WebRefreshTokenDto>.Success(webResponse));
            }

            // Mobile: Return both tokens in JSON body
            return Ok(ApiResponse<RefreshTokenDto>.Success(newTokens));
        }

        return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
    }

    /// <summary>
    /// Logout and revoke refresh token.
    /// For web browsers: Clears refresh token cookie.
    /// For mobile apps: Revokes token from request body.
    /// Client should also discard access token locally.
    /// </summary>
    /// <param name="command">Refresh token to revoke (optional for web browsers)</param>
    /// <param name="ct">Cancellation token</param>
    /// <response code="200">Logged out successfully</response>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout(
        [FromBody(EmptyBodyBehavior = Microsoft.AspNetCore.Mvc.ModelBinding.EmptyBodyBehavior.Allow)] LogoutCommand? command,
        CancellationToken ct)
    {
        string? refreshToken = null;

        // Try cookie first (web browsers)
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var cookieToken))
        {
            refreshToken = cookieToken;
        }
        // Fallback to request body (mobile apps)
        else if (command?.RefreshToken is not null)
        {
            refreshToken = command.RefreshToken;
        }

        if (!string.IsNullOrEmpty(refreshToken))
        {
            var logoutCommand = new LogoutCommand { RefreshToken = refreshToken };
            var result = await sender.Send(logoutCommand, ct);

            if (!result.Succeeded)
            {
                return BadRequest(ApiResponse.Failure(result.Errors, result.ErrorCode));
            }
        }

        // Clear cookie if exists (web browsers)
        if (Request.Cookies.ContainsKey(RefreshTokenCookieName))
        {
            ClearRefreshTokenCookie();
        }

        return Ok(ApiResponse.Success());
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
