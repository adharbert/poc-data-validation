using POC.CustomerValidation.API.Models.DTOs;
using System.Net;
using System.Text.Json;

namespace POC.CustomerValidation.API.Middleware
{
    public class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> log)
    {

        private readonly RequestDelegate _next = next;
        private readonly ILogger<ExceptionHandlingMiddleware> _log = log;


        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await HandleAsync(context, ex);
            }
        }




        private async Task HandleAsync(HttpContext context, Exception ex)
        {
            var (statusCode, errorCode, message) = ex switch
            {
                KeyNotFoundException =>         (HttpStatusCode.NotFound,               "NOT_FOUND", ex.Message),
                InvalidOperationException =>    (HttpStatusCode.Conflict,               "CONFLICT", ex.Message),
                UnauthorizedAccessException =>  (HttpStatusCode.Forbidden,              "FORBIDDEN", ex.Message),
                ArgumentException =>            (HttpStatusCode.BadRequest,             "BAD_REQUEST", ex.Message),
                _ =>                            (HttpStatusCode.InternalServerError,    "INTERNAL_ERROR", "An unexpected error occurred. Please try again later.")
            };

            if (statusCode == HttpStatusCode.InternalServerError)
                _log.LogError(ex, "Unhandled exception on {Method} {Path}",
                    context.Request.Method, context.Request.Path);
            else
                _log.LogWarning("Handled exception {ErrorCode} on {Method} {Path}: {Message}",
                    errorCode, context.Request.Method, context.Request.Path, ex.Message);

            context.Response.StatusCode = (int)statusCode;
            context.Response.ContentType = "application/json";

            var error = new ApiError(errorCode, message);
            var json = JsonSerializer.Serialize(error, _jsonOptions);

            await context.Response.WriteAsync(json);
        }




        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

    }
}
