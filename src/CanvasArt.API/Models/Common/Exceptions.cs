namespace CanvasArt.API.Models.Common;

/// <summary>Base class for all expected/handled application errors.</summary>
public abstract class AppException : Exception
{
    public int StatusCode { get; }
    public IReadOnlyList<string>? Errors { get; }

    protected AppException(string message, int statusCode, IReadOnlyList<string>? errors = null)
        : base(message)
    {
        StatusCode = statusCode;
        Errors = errors;
    }
}

/// <summary>404 — the requested resource does not exist.</summary>
public sealed class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message, 404) { }

    public NotFoundException(string entity, object key)
        : base($"{entity} with identifier '{key}' was not found.", 404) { }
}

/// <summary>400 — the request failed validation.</summary>
public sealed class ValidationException : AppException
{
    public ValidationException(IReadOnlyList<string> errors)
        : base("One or more validation errors occurred.", 400, errors) { }

    public ValidationException(string error)
        : base("One or more validation errors occurred.", 400, new[] { error }) { }
}

/// <summary>409 — the request conflicts with the current state (e.g. duplicate unique key).</summary>
public sealed class ConflictException : AppException
{
    public ConflictException(string message) : base(message, 409) { }
}

/// <summary>401 — authentication is required or the supplied credentials are invalid.</summary>
public sealed class UnauthorizedException : AppException
{
    public UnauthorizedException(string message = "Unauthorized.") : base(message, 401) { }
}

/// <summary>403 — the caller is authenticated but not allowed to perform the action.</summary>
public sealed class ForbiddenException : AppException
{
    public ForbiddenException(string message = "Forbidden.") : base(message, 403) { }
}
