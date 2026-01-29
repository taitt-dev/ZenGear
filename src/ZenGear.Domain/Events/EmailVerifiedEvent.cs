using ZenGear.Domain.Common;

namespace ZenGear.Domain.Events;

/// <summary>
/// Domain event raised when a user verifies their email.
/// </summary>
/// <param name="UserId">Internal user ID</param>
/// <param name="UserExternalId">External user ID (e.g., usr_xxx)</param>
/// <param name="Email">User email</param>
public record EmailVerifiedEvent(
    long UserId,
    string UserExternalId,
    string Email) : IDomainEvent;
