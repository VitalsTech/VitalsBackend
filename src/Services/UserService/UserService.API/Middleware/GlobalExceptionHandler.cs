using System.Text.Json;
using FluentValidation;
using UserService.Application.Exceptions;

namespace UserService.API.Middleware
{
    public class GlobalExceptionHandler
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
                await HandleExceptionAsync(context, ex);
            }
        }

        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            _logger.LogError(exception, "An unhandled exception occurred");

            var response = context.Response;
            response.ContentType = "application/json";

            object errorResponse;

            switch (exception)
            {
                case ValidationException validationEx:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    errorResponse = new { error = validationEx.Message, errors = validationEx.Errors.Select(e => e.ErrorMessage) };
                    break;

                case DuplicatePhoneException:
                    response.StatusCode = StatusCodes.Status409Conflict;
                    errorResponse = new { error = exception.Message };
                    break;

                case DuplicateEmailException:
                    response.StatusCode = StatusCodes.Status409Conflict;
                    errorResponse = new { error = exception.Message };
                    break;

                case UserNotFoundException:
                    response.StatusCode = StatusCodes.Status404NotFound;
                    errorResponse = new { error = exception.Message };
                    break;

                case ArgumentException:
                    response.StatusCode = StatusCodes.Status400BadRequest;
                    errorResponse = new { error = exception.Message };
                    break;

                default:
                    response.StatusCode = StatusCodes.Status500InternalServerError;
                    errorResponse = new { error = "An internal error occurred. Please try again later." };
                    break;
            }

            var json = System.Text.Json.JsonSerializer.Serialize(errorResponse);
            await response.WriteAsync(json);
        }
    }
}