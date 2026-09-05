using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace PlumeriaStore.Api.Common.Errors;

/// <summary>
/// Maps domain exceptions to RFC 7807 ProblemDetails responses. Anything not recognized here
/// falls through to ASP.NET Core's default problem-details handling (500).
/// </summary>
public class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetailsService = problemDetailsService;
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

        // Written through the framework's own problem-details service rather than serialized here,
        // so the payload matches what Results.Problem produces and stays on the source-generated
        // JSON path Native AOT needs.
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
            },
        });
    }
}
