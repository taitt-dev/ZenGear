namespace ZenGear.Domain.Constants;

/// <summary>
/// External ID prefixes for each entity type.
/// Format: {prefix}_{nanoid16}
/// Example: usr_V1StGXR8Z5jdHi6B
/// </summary>
public static class EntityPrefixes
{
    /// <summary>
    /// User entity prefix (usr_xxx).
    /// </summary>
    public const string User = "usr";

    /// <summary>
    /// Product entity prefix (prod_xxx).
    /// </summary>
    public const string Product = "prod";

    /// <summary>
    /// Product Variant entity prefix (var_xxx).
    /// </summary>
    public const string ProductVariant = "var";

    /// <summary>
    /// Product Image entity prefix (img_xxx).
    /// </summary>
    public const string ProductImage = "img";

    /// <summary>
    /// Category entity prefix (cat_xxx).
    /// </summary>
    public const string Category = "cat";

    /// <summary>
    /// Brand entity prefix (brd_xxx).
    /// </summary>
    public const string Brand = "brd";

    /// <summary>
    /// User Address entity prefix (adr_xxx).
    /// </summary>
    public const string Address = "adr";

    /// <summary>
    /// Cart Item entity prefix (cit_xxx).
    /// </summary>
    public const string CartItem = "cit";

    /// <summary>
    /// Order entity prefix (ord_xxx).
    /// </summary>
    public const string Order = "ord";

    /// <summary>
    /// Review entity prefix (rev_xxx).
    /// </summary>
    public const string Review = "rev";

    /// <summary>
    /// Coupon entity prefix (cpn_xxx).
    /// </summary>
    public const string Coupon = "cpn";

    /// <summary>
    /// Promotion entity prefix (prm_xxx).
    /// </summary>
    public const string Promotion = "prm";

    /// <summary>
    /// Wishlist Item entity prefix (wit_xxx).
    /// </summary>
    public const string WishlistItem = "wit";
}
