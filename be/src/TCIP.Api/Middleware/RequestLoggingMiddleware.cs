using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace TCIP.Api.Middleware;

public sealed class RequestLoggingMiddleware(
    RequestDelegate next,
    ILogger<RequestLoggingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var startedAt = Stopwatch.GetTimestamp();

        context.Response.OnStarting(() =>
        {
            context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier;
            return Task.CompletedTask;
        });

        try
        {
            await next(context);
        }
        finally
        {
            Guid.TryParse(context.User.FindFirst("sub")?.Value, out var userId);
            logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {ElapsedMs:F1} ms; TraceId={TraceId}; UserId={UserId}",
                context.Request.Method,
                context.Request.Path.Value,
                context.Response.StatusCode,
                Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds,
                context.TraceIdentifier,
                userId == Guid.Empty ? null : userId);
        }
    }
}
