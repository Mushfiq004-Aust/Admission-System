using api.DTOs.Common;
using api.Exceptions;

namespace api.Middleware
{
    // Sits in front of every request. If anything throws:
    //   - an AppException (expected error)  -> its specific 4xx JSON response
    //   - anything else (a real bug)        -> a generic 500, no internal details leaked
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (AppException ex)
            {
                await WriteAsync(context, ex.StatusCode, ex.Code, ex.Message);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.Response.StatusCode = 499; // the browser went away, nobody is listening
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
                await WriteAsync(context, StatusCodes.Status500InternalServerError,
                    "INTERNAL_ERROR", "An unexpected error occurred. Please try again.");
            }
        }

        private static async Task WriteAsync(HttpContext context, int status, string code, string message)
        {
            if (context.Response.HasStarted) return;
            context.Response.Clear();
            context.Response.StatusCode = status;
            await context.Response.WriteAsJsonAsync(new ErrorInView(code, message));
        }
    }
}
