using Serilog;
using System.Text;

namespace POC.CustomerValidation.API.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next)
{
    private const string CorrelationHeader = "X-Correlation-Id";
    private readonly RequestDelegate _next = next;


    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = Guid.NewGuid().ToString();

        context.Request.EnableBuffering();

        string requestBody = "";

        if (context.Request.ContentLength > 0 &&
            context.Request.Body.CanRead)
        {
            using var reader = new StreamReader(
                context.Request.Body,
                Encoding.UTF8,
                leaveOpen: true);

            requestBody = await reader.ReadToEndAsync();

            context.Request.Body.Position = 0;
        }

        using (Serilog.Context.LogContext.PushProperty("CorrelationId", correlationId))
        using (Serilog.Context.LogContext.PushProperty("RequestPath", context.Request.Path))
        using (Serilog.Context.LogContext.PushProperty("RequestMethod", context.Request.Method))
        using (Serilog.Context.LogContext.PushProperty("RequestBody", requestBody))
        {
            await _next(context);
        }
    }
}
