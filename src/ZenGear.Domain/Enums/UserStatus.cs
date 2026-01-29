namespace ZenGear.Domain.Enums;

/// <summary>
/// User account status.
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Account is active and can login.
    /// </summary>
    Active = 1,

    /// <summary>
    /// Account is inactive (e.g., self-deactivated).
    /// Cannot login.
    /// </summary>
    Inactive = 2,

    /// <summary>
    /// Account is banned by admin.
    /// Cannot login.
    /// </summary>
    Banned = 3
}
