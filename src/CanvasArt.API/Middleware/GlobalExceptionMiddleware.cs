using System.Text.Json;
using CanvasArt.API.Models.Common;

namespace CanvasArt.API.Middleware;

/// <summary>
/// Converts unhandled and expected (<see cref="AppException"/>) errors into the standard
/// <see cref="ApiResponse{T}"/> envelope with the correct HTTP status code.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Handled application error: {Message}", ex.Message);
            await WriteAsync(context, ex.StatusCode, ex.Message, ex.Errors);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Client disconnected; nothing to write.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            var message = _env.IsDevelopment() ? ex.Message : "An unexpected error occurred.";
            var errors = _env.IsDevelopment() ? new[] { ex.ToString() } : null;
            await WriteAsync(context, StatusCodes.Status500InternalServerError, message, errors);
        }
    }

    private static async Task WriteAsync(HttpContext context, int statusCode, string message, IReadOnlyList<string>? errors)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var payload = ApiResponse<object?>.Fail(message, errors);
        await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
    }
}
