using System.Text.Json.Serialization;

namespace ZenGear.Application.Common.Models;

/// <summary>
/// Standardized API response wrapper with data.
/// </summary>
/// <typeparam name="T">Type of data in the response</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request succeeded.
    /// </summary>
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; init; }

    /// <summary>
    /// Response data. Null if request failed.
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    /// <summary>
    /// Error messages if request failed. Empty if succeeded.
    /// </summary>
    [JsonPropertyName("errors")]
    public string[] Errors { get; init; } = [];

    /// <summary>
    /// Error code for client-side error handling. Null if succeeded.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Timestamp when the response was generated (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Create a successful response with data.
    /// </summary>
    public static ApiResponse<T> Success(T data)
    {
        return new ApiResponse<T>
        {
            Succeeded = true,
            Data = data,
            Errors = [],
            ErrorCode = null
        };
    }

    /// <summary>
    /// Create a failed response with single error.
    /// </summary>
    public static ApiResponse<T> Failure(string error, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Data = default,
            Errors = [error],
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Create a failed response with multiple errors.
    /// </summary>
    public static ApiResponse<T> Failure(string[] errors, string? errorCode = null)
    {
        return new ApiResponse<T>
        {
            Succeeded = false,
            Data = default,
            Errors = errors,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Standardized API response wrapper without data.
/// Use for operations that don't return data (e.g., Delete, Logout).
/// </summary>
public class ApiResponse
{
    /// <summary>
    /// Indicates whether the request succeeded.
    /// </summary>
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; init; }

    /// <summary>
    /// Error messages if request failed. Empty if succeeded.
    /// </summary>
    [JsonPropertyName("errors")]
    public string[] Errors { get; init; } = [];

    /// <summary>
    /// Error code for client-side error handling. Null if succeeded.
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Timestamp when the response was generated (UTC).
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Create a successful response.
    /// </summary>
    public static ApiResponse Success()
    {
        return new ApiResponse
        {
            Succeeded = true,
            Errors = [],
            ErrorCode = null
        };
    }

    /// <summary>
    /// Create a failed response with single error.
    /// </summary>
    public static ApiResponse Failure(string error, string? errorCode = null)
    {
        return new ApiResponse
        {
            Succeeded = false,
            Errors = [error],
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Create a failed response with multiple errors.
    /// </summary>
    public static ApiResponse Failure(string[] errors, string? errorCode = null)
    {
        return new ApiResponse
        {
            Succeeded = false,
            Errors = errors,
            ErrorCode = errorCode
        };
    }
}
