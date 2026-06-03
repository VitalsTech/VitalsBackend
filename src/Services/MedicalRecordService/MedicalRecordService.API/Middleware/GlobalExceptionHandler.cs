using MedicalRecordService.Application.Exceptions;

namespace MedicalRecordService.API.Middleware;

public sealed class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
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
        catch (Exception ex)
        {
            await HandleAsync(context, ex);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        _logger.LogError(exception, "Medical record error");

        context.Response.ContentType = "application/json";
        var (status, message) = exception switch
        {
            MedicalRecordValidationException e => (StatusCodes.Status400BadRequest, e.Message),
            AccessDeniedException e => (StatusCodes.Status403Forbidden, e.Message),
            DuplicateEventException e => (StatusCodes.Status409Conflict, e.Message),
            _ => (StatusCodes.Status500InternalServerError, "An internal error occurred.")
        };

        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new { error = message });
    }
}
