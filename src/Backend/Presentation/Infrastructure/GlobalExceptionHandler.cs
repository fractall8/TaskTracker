using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "An exception occurred: {Message}", exception.Message);

        var (statusCode, title, detail) = exception switch
        {
            ValidationException => 
                (StatusCodes.Status400BadRequest, "Validation Error", "One or more validation errors occurred."),
            
            KeyNotFoundException => 
                (StatusCodes.Status404NotFound, "Not Found", exception.Message),
            
            UnauthorizedAccessException => 
                (StatusCodes.Status403Forbidden, "Forbidden", exception.Message),
            
            InvalidOperationException => 
                (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            
            // Fallback for all other
            _ => 
                (StatusCodes.Status500InternalServerError, "Server Error", "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        if (exception is ValidationException validationException)
        {
            var errors = validationException.Errors
                .GroupBy(x => x.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(x => x.ErrorMessage).ToArray()
                );
                
            problemDetails.Extensions.Add("errors", errors);
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true; 
    }
}