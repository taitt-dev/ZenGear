namespace ZenGear.Application.Common.Models;

/// <summary>
/// Result pattern for operations that can succeed or fail.
/// Use Result&lt;T&gt; for operations that return data on success.
/// </summary>
/// <typeparam name="T">Type of data returned on success</typeparam>
public class Result<T>
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Data returned on success. Null if failed.
    /// </summary>
    public T? Data { get; }

    /// <summary>
    /// Error messages if operation failed. Empty if succeeded.
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Error code for client handling. Null if succeeded.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Private constructor. Use Success() or Failure() factory methods.
    /// </summary>
    private Result(bool succeeded, T? data, string[] errors, string? errorCode)
    {
        Succeeded = succeeded;
        Data = data;
        Errors = errors;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Create a successful result with data.
    /// </summary>
    public static Result<T> Success(T data)
    {
        return new Result<T>(true, data, [], null);
    }

    /// <summary>
    /// Create a failed result with single error message.
    /// </summary>
    public static Result<T> Failure(string error, string? errorCode = null)
    {
        return new Result<T>(false, default, [error], errorCode);
    }

    /// <summary>
    /// Create a failed result with multiple error messages.
    /// </summary>
    public static Result<T> Failure(string[] errors, string? errorCode = null)
    {
        return new Result<T>(false, default, errors, errorCode);
    }

    /// <summary>
    /// Pattern matching for result handling.
    /// </summary>
    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string[], string?, TResult> onFailure)
    {
        return Succeeded
            ? onSuccess(Data!)
            : onFailure(Errors, ErrorCode);
    }

    /// <summary>
    /// Implicit conversion from T to Result&lt;T&gt;.
    /// </summary>
    public static implicit operator Result<T>(T data) => Success(data);
}

/// <summary>
/// Result pattern for operations that don't return data.
/// </summary>
public class Result
{
    /// <summary>
    /// Indicates whether the operation succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Error messages if operation failed. Empty if succeeded.
    /// </summary>
    public string[] Errors { get; }

    /// <summary>
    /// Error code for client handling. Null if succeeded.
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// Private constructor. Use Success() or Failure() factory methods.
    /// </summary>
    private Result(bool succeeded, string[] errors, string? errorCode)
    {
        Succeeded = succeeded;
        Errors = errors;
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Create a successful result.
    /// </summary>
    public static Result Success()
    {
        return new Result(true, [], null);
    }

    /// <summary>
    /// Create a failed result with single error message.
    /// </summary>
    public static Result Failure(string error, string? errorCode = null)
    {
        return new Result(false, [error], errorCode);
    }

    /// <summary>
    /// Create a failed result with multiple error messages.
    /// </summary>
    public static Result Failure(string[] errors, string? errorCode = null)
    {
        return new Result(false, errors, errorCode);
    }

    /// <summary>
    /// Pattern matching for result handling.
    /// </summary>
    public TResult Match<TResult>(
        Func<TResult> onSuccess,
        Func<string[], string?, TResult> onFailure)
    {
        return Succeeded
            ? onSuccess()
            : onFailure(Errors, ErrorCode);
    }
}
