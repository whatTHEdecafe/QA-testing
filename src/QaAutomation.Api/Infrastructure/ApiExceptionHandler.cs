using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using QaAutomation.Core.Targets;

namespace QaAutomation.Api.Infrastructure;

public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is DomainValidationException validationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = context,
                ProblemDetails = new HttpValidationProblemDetails(validationException.Errors)
                { Title = "Validation failed", Status = StatusCodes.Status400BadRequest },
                Exception = exception
            });
        }

        logger.LogError(exception, "Unhandled request failure for {Method} {Path}",
            context.Request.Method, context.Request.Path);
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Title = "An unexpected error occurred",
                Detail = "The request could not be completed. Check the application logs for technical details.",
                Status = StatusCodes.Status500InternalServerError
            },
            Exception = exception
        });
    }
}
