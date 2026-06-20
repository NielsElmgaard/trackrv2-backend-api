using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace BadmintonKiosken.Core.Middleware;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            KeyNotFoundException => StatusCodes.Status404NotFound,
            BadHttpRequestException or InvalidOperationException => StatusCodes
                .Status400BadRequest,
            UnauthorizedAccessException or SecurityTokenException => StatusCodes
                .Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError
        };

        httpContext.Response.StatusCode = statusCode;

        if (statusCode >= 500)
        {
            logger.LogError(exception,
                "En kritisk fejl opstod på {Path} {Method}",
                httpContext.Request.Path,
                httpContext.Request.Method);
        }
        else
        {
            // Brugerfejl
            logger.LogWarning(
                "Request fejlede med {StatusCode}: {Message} på {Path}",
                statusCode,
                exception.Message,
                httpContext.Request.Path);
        }

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Type = exception.GetType().Name,
                    Title = statusCode switch
                    {
                        StatusCodes.Status404NotFound =>
                            "Ressourcen blev ikke fundet",
                        StatusCodes.Status401Unauthorized => "Ikke autoriseret",
                        StatusCodes.Status400BadRequest => "Ugyldig anmodning",
                        _ => "En uventet fejl opstod"
                    },
                    Status = statusCode,
                    Detail = statusCode ==
                             StatusCodes.Status500InternalServerError
                        ? "Kontakt venligst support, hvis problemet fortsætter."
                        : exception.Message,
                    Instance = httpContext.Request.Path
                }
            });
    }
}