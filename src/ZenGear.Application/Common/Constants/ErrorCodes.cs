namespace ZenGear.Application.Common.Constants;

/// <summary>
/// Error codes for API responses.
/// Format: ENTITY_ACTION_ERROR in SCREAMING_SNAKE_CASE
/// </summary>
public static class ErrorCodes
{
    // Generic errors
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string InternalError = "INTERNAL_ERROR";

    /// <summary>
    /// Authentication and authorization error codes.
    /// </summary>
    public static class Auth
    {
        public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
        public const string EmailNotConfirmed = "AUTH_EMAIL_NOT_CONFIRMED";
        public const string AccountLocked = "AUTH_ACCOUNT_LOCKED";
        public const string AccountInactive = "AUTH_ACCOUNT_INACTIVE";
        public const string InvalidToken = "AUTH_INVALID_TOKEN";
        public const string TokenExpired = "AUTH_TOKEN_EXPIRED";
        public const string RefreshTokenExpired = "AUTH_REFRESH_TOKEN_EXPIRED";
        public const string RefreshTokenRevoked = "AUTH_REFRESH_TOKEN_REVOKED";
        public const string OtpInvalid = "AUTH_OTP_INVALID";
        public const string OtpExpired = "AUTH_OTP_EXPIRED";
        public const string EmailAlreadyExists = "AUTH_EMAIL_ALREADY_EXISTS";
        public const string WeakPassword = "AUTH_WEAK_PASSWORD";
        public const string SocialLoginFailed = "AUTH_SOCIAL_LOGIN_FAILED";
        public const string RegistrationFailed = "AUTH_REGISTRATION_FAILED";
    }

    /// <summary>
    /// User-related error codes.
    /// </summary>
    public static class User
    {
        public const string NotFound = "USER_NOT_FOUND";
        public const string AlreadyExists = "USER_ALREADY_EXISTS";
        public const string CannotUpdate = "USER_CANNOT_UPDATE";
        public const string CannotDelete = "USER_CANNOT_DELETE";
        public const string InvalidCredentials = "USER_INVALID_CREDENTIALS";
        public const string EmailNotVerified = "USER_EMAIL_NOT_VERIFIED";
        public const string EmailAlreadyVerified = "USER_EMAIL_ALREADY_VERIFIED";
        public const string AccountLocked = "USER_ACCOUNT_LOCKED";
        public const string InvalidOtpCode = "USER_INVALID_OTP_CODE";
        public const string OtpRateLimitExceeded = "USER_OTP_RATE_LIMIT_EXCEEDED";
        public const string EmailVerificationFailed = "USER_EMAIL_VERIFICATION_FAILED";
        public const string RegistrationFailed = "USER_REGISTRATION_FAILED";
        public const string PasswordChangeFailed = "USER_PASSWORD_CHANGE_FAILED";
        public const string PasswordResetFailed = "USER_PASSWORD_RESET_FAILED";
        public const string InvalidRefreshToken = "USER_INVALID_REFRESH_TOKEN";
        public const string RefreshTokenExpired = "USER_REFRESH_TOKEN_EXPIRED";
        public const string EmailSendFailed = "USER_EMAIL_SEND_FAILED";
    }

    /// <summary>
    /// Product-related error codes (placeholder for future features).
    /// </summary>
    public static class Product
    {
        public const string NotFound = "PRODUCT_NOT_FOUND";
        public const string SlugAlreadyExists = "PRODUCT_SLUG_ALREADY_EXISTS";
        public const string CannotPublish = "PRODUCT_CANNOT_PUBLISH";
        public const string NoVariants = "PRODUCT_NO_VARIANTS";
        public const string VariantNotFound = "PRODUCT_VARIANT_NOT_FOUND";
    }

    /// <summary>
    /// Category-related error codes (placeholder for future features).
    /// </summary>
    public static class Category
    {
        public const string NotFound = "CATEGORY_NOT_FOUND";
        public const string AlreadyExists = "CATEGORY_ALREADY_EXISTS";
    }

    /// <summary>
    /// Brand-related error codes (placeholder for future features).
    /// </summary>
    public static class Brand
    {
        public const string NotFound = "BRAND_NOT_FOUND";
        public const string AlreadyExists = "BRAND_ALREADY_EXISTS";
    }

    /// <summary>
    /// Cart-related error codes (placeholder for future features).
    /// </summary>
    public static class Cart
    {
        public const string Empty = "CART_EMPTY";
        public const string InsufficientStock = "CART_INSUFFICIENT_STOCK";
        public const string ItemNotFound = "CART_ITEM_NOT_FOUND";
    }

    /// <summary>
    /// Order-related error codes (placeholder for future features).
    /// </summary>
    public static class Order
    {
        public const string NotFound = "ORDER_NOT_FOUND";
        public const string CannotCancel = "ORDER_CANNOT_CANCEL";
        public const string MinimumNotMet = "ORDER_MINIMUM_NOT_MET";
    }
}
