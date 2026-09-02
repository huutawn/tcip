using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TCIP.Common.Exceptions;

namespace TCIP.Api.ExceptionHandling;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            UnauthorizedException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            BadRequestException => (StatusCodes.Status400BadRequest, "Bad Request"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            UnauthenticationException unauth => (StatusCodes.Status401Unauthorized, unauth.Message),
            PreconditionRequiredException => (StatusCodes.Status428PreconditionRequired, "Precondition Required"),
            PreconditionFailedException => (StatusCodes.Status412PreconditionFailed, "Precondition Failed"),
            DbUpdateConcurrencyException => (StatusCodes.Status412PreconditionFailed, "Precondition Failed"),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            logger.LogWarning(exception, "Request failed with status code {StatusCode}: {Message}", statusCode, exception.Message);
        }

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = statusCode >= 500 ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }
}
