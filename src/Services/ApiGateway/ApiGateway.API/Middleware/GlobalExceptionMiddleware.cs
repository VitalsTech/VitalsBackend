namespace ApiGateway.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var errorId = Guid.NewGuid().ToString("N");
            _logger.LogError(ex, "Gateway error {ErrorId} [{RequestId}]", errorId, context.TraceIdentifier);

            if (context.Response.HasStarted)
                throw;

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            if (_environment.IsDevelopment())
            {
                await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера.", errorId, detail = ex.Message });
            }
            else
            {
                await context.Response.WriteAsJsonAsync(new { error = "Внутренняя ошибка сервера.", errorId });
            }
        }
    }
}
