namespace CanvasArt.API.Models.Common;

/// <summary>
/// Standard response envelope returned by every endpoint:
/// <c>{ success, message, data, errors }</c>.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public IReadOnlyList<string>? Errors { get; set; }

    public static ApiResponse<T> Ok(T data, string message = "Request succeeded.") =>
        new() { Success = true, Message = message, Data = data };

    public static ApiResponse<T> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Data = default, Errors = errors };
}

/// <summary>Non-generic helpers for responses that carry no payload.</summary>
public static class ApiResponse
{
    public static ApiResponse<object?> Ok(string message = "Request succeeded.") =>
        new() { Success = true, Message = message, Data = null };

    public static ApiResponse<object?> Fail(string message, IReadOnlyList<string>? errors = null) =>
        new() { Success = false, Message = message, Data = null, Errors = errors };
}
