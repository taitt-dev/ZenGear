namespace ZenGear.Application.Common.Constants;

/// <summary>
/// Role names for authorization.
/// </summary>
public static class Roles
{
    /// <summary>
    /// Administrator role - full system access.
    /// </summary>
    public const string Admin = "Admin";

    /// <summary>
    /// Manager role - can manage products, orders, users.
    /// </summary>
    public const string Manager = "Manager";

    /// <summary>
    /// Staff role - can process orders, manage inventory.
    /// </summary>
    public const string Staff = "Staff";

    /// <summary>
    /// Customer role - can shop, order, review.
    /// </summary>
    public const string Customer = "Customer";

    /// <summary>
    /// Combined roles for authorization.
    /// </summary>
    public static class Combined
    {
        /// <summary>
        /// Admin or Manager roles.
        /// </summary>
        public const string AdminOrManager = "Admin,Manager";

        /// <summary>
        /// Staff or above (Admin, Manager, Staff).
        /// </summary>
        public const string StaffOrAbove = "Admin,Manager,Staff";
    }
}
