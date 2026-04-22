using POC.CustomerValidation.API.Middleware;

namespace POC.CustomerValidation.API.Extensions;

public static class RequestLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseCustomRequestLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RequestLoggingMiddleware>();
    }
}
