using System.Diagnostics;

namespace Ego.Api.Middleware;

public class RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await next(context);
        }
        finally
        {
            stopwatch.Stop();

            logger.LogInformation(
                "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs}ms (TraceId: {TraceId})",
                context.Request.Method,
                context.Request.Path,
                context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty,
                context.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                context.TraceIdentifier);
        }
    }
}
