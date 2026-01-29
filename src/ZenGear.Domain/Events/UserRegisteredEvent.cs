using ZenGear.Domain.Common;

namespace ZenGear.Domain.Events;

/// <summary>
/// Domain event raised when a new user registers.
/// </summary>
/// <param name="UserId">Internal user ID</param>
/// <param name="UserExternalId">External user ID (e.g., usr_xxx)</param>
/// <param name="Email">User email</param>
/// <param name="FirstName">User first name</param>
/// <param name="LastName">User last name</param>
public record UserRegisteredEvent(
    long UserId,
    string UserExternalId,
    string Email,
    string FirstName,
    string LastName) : IDomainEvent;
