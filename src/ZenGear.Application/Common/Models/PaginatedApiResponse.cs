using System.Text.Json.Serialization;

namespace ZenGear.Application.Common.Models;

/// <summary>
/// Standardized API response for paginated data.
/// </summary>
/// <typeparam name="T">Type of items in the paginated list</typeparam>
public class PaginatedApiResponse<T>
{
    /// <summary>
    /// Indicates whether the request succeeded.
    /// </summary>
    [JsonPropertyName("succeeded")]
    public bool Succeeded { get; init; }

    /// <summary>
    /// Paginated data. Null if request failed.
    /// </summary>
    [JsonPropertyName("data")]
    public PaginatedData<T>? Data { get; init; }

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
    /// Create a successful paginated response.
    /// </summary>
    public static PaginatedApiResponse<T> Success(PaginatedList<T> paginatedList)
    {
        return new PaginatedApiResponse<T>
        {
            Succeeded = true,
            Data = new PaginatedData<T>
            {
                Items = paginatedList.Items,
                PageNumber = paginatedList.PageNumber,
                PageSize = paginatedList.PageSize,
                TotalCount = paginatedList.TotalCount,
                TotalPages = paginatedList.TotalPages,
                HasPreviousPage = paginatedList.HasPreviousPage,
                HasNextPage = paginatedList.HasNextPage
            },
            Errors = [],
            ErrorCode = null
        };
    }

    /// <summary>
    /// Create a failed paginated response.
    /// </summary>
    public static PaginatedApiResponse<T> Failure(string error, string? errorCode = null)
    {
        return new PaginatedApiResponse<T>
        {
            Succeeded = false,
            Data = null,
            Errors = [error],
            ErrorCode = errorCode
        };
    }

    /// <summary>
    /// Create a failed paginated response with multiple errors.
    /// </summary>
    public static PaginatedApiResponse<T> Failure(string[] errors, string? errorCode = null)
    {
        return new PaginatedApiResponse<T>
        {
            Succeeded = false,
            Data = null,
            Errors = errors,
            ErrorCode = errorCode
        };
    }
}

/// <summary>
/// Paginated data structure for API responses.
/// </summary>
public class PaginatedData<T>
{
    [JsonPropertyName("items")]
    public required IReadOnlyList<T> Items { get; init; }

    [JsonPropertyName("pageNumber")]
    public required int PageNumber { get; init; }

    [JsonPropertyName("pageSize")]
    public required int PageSize { get; init; }

    [JsonPropertyName("totalCount")]
    public required int TotalCount { get; init; }

    [JsonPropertyName("totalPages")]
    public required int TotalPages { get; init; }

    [JsonPropertyName("hasPreviousPage")]
    public required bool HasPreviousPage { get; init; }

    [JsonPropertyName("hasNextPage")]
    public required bool HasNextPage { get; init; }
}
