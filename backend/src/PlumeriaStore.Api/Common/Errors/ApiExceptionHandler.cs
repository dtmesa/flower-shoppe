using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PlumeriaStore.Api.Common.Errors;

/// <summary>
/// Maps domain exceptions to RFC 7807 ProblemDetails responses. Anything not recognized here
/// falls through to ASP.NET Core's default problem-details handling (500).
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(ILogger<ApiExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, exception.Message),
            BadRequestException => (StatusCodes.Status400BadRequest, exception.Message),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            _logger.LogError(exception, "Unhandled exception");
            return false;
        }

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status,
            Title = title,
        }, cancellationToken);

        return true;
    }
}
