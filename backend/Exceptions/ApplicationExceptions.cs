namespace api.Exceptions
{
    // Base type for "expected" errors that should become a specific 4xx response.
    // Services throw these; ExceptionHandlingMiddleware turns them into JSON.
    public abstract class AppException : Exception
    {
        public int StatusCode { get; }
        public string Code { get; }

        protected AppException(int statusCode, string code, string message) : base(message)
        {
            StatusCode = statusCode;
            Code = code;
        }
    }

    // 400
    public sealed class BadRequestException : AppException
    {
        public BadRequestException(string code, string message) : base(StatusCodes.Status400BadRequest, code, message) { }
    }

    // 401
    public sealed class UnauthorizedException : AppException
    {
        public UnauthorizedException(string code, string message) : base(StatusCodes.Status401Unauthorized, code, message) { }
    }

    // 403
    public sealed class ForbiddenException : AppException
    {
        public ForbiddenException(string code, string message) : base(StatusCodes.Status403Forbidden, code, message) { }
    }

    // 404
    public sealed class NotFoundException : AppException
    {
        public NotFoundException(string code, string message) : base(StatusCodes.Status404NotFound, code, message) { }
    }

    // 409
    public sealed class ConflictException : AppException
    {
        public ConflictException(string code, string message) : base(StatusCodes.Status409Conflict, code, message) { }
    }

    // 503
    public sealed class ServiceUnavailableException : AppException
    {
        public ServiceUnavailableException(string code, string message) : base(StatusCodes.Status503ServiceUnavailable, code, message) { }
    }
}
