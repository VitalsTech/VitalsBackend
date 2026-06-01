using AuthService.Application.Exceptions;

namespace AuthService.API.Middleware;

public sealed class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment)
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
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Unhandled auth exception");

        context.Response.ContentType = "application/json";
        var (status, message) = exception switch
        {
            AuthValidationException e => (StatusCodes.Status400BadRequest, e.Message),
            InvalidCredentialsException e => (StatusCodes.Status401Unauthorized, e.Message),
            AccountBlockedException e => (StatusCodes.Status403Forbidden, e.Message),
            DuplicatePhoneException e => (StatusCodes.Status409Conflict, e.Message),
            InvalidRefreshTokenException e => (StatusCodes.Status401Unauthorized, e.Message),
            EsiaNotConfiguredException e => (StatusCodes.Status503ServiceUnavailable, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal error occurred.")
        };

        context.Response.StatusCode = status;
        object payload = status == StatusCodes.Status500InternalServerError
            ? new
            {
                error = message,
                errorId = Guid.NewGuid().ToString("N"),
                detail = _environment.IsDevelopment() ? exception.Message : null
            }
            : new { error = message };

        await context.Response.WriteAsJsonAsync(payload);
    }
}
