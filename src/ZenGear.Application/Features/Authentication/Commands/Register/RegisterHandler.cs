using MediatR;
using ZenGear.Application.Common.Constants;
using ZenGear.Application.Common.Interfaces;
using ZenGear.Application.Common.Models;
using ZenGear.Domain.Common;
using ZenGear.Domain.Constants;
using ZenGear.Domain.Enums;

namespace ZenGear.Application.Features.Authentication.Commands.Register;

/// <summary>
/// Handler for RegisterCommand.
/// Creates user account and sends email verification OTP.
/// </summary>
public class RegisterHandler(
    IIdentityService identityService,
    IOtpService otpService,
    IEmailService emailService,
    IExternalIdGenerator externalIdGenerator)
    : IRequestHandler<RegisterCommand, Result>
{
    public async Task<Result> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Generate ExternalId
        var externalId = externalIdGenerator.Generate(EntityPrefixes.User);

        // Create user
        var (succeeded, userId, errors) = await identityService.CreateUserAsync(
            externalId,
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            ct);

        if (!succeeded)
        {
            return Result.Failure(errors, ErrorCodes.User.RegistrationFailed);
        }

        // Generate and send email verification OTP
        try
        {
            var otpCode = await otpService.CreateOtpAsync(
                userId,
                OtpPurpose.EmailVerification,
                ct);

            await emailService.SendEmailVerificationAsync(
                request.Email,
                request.FirstName,
                otpCode,
                ct);
        }
        catch
        {
            // Log error but don't fail registration
            // User can request OTP resend later
            // TODO: Add logging
        }

        return Result.Success();
    }
}
