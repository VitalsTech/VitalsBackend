using System.Diagnostics;

namespace ApiGateway.API.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var sw = Stopwatch.StartNew();
        await _next(context);
        sw.Stop();

        _logger.LogInformation(
            "HTTP {Method} {Path} -> {Status} in {ElapsedMs}ms from {Ip} [{RequestId}]",
            context.Request.Method,
            context.Request.Path.Value ?? "/",
            context.Response.StatusCode,
            sw.ElapsedMilliseconds,
            context.Connection.RemoteIpAddress,
            context.TraceIdentifier);
    }
}
