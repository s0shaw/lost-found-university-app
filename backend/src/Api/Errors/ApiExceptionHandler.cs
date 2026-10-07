using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Api.Errors;

/// <summary>
/// Single place that turns known exceptions into RFC 9457 ProblemDetails responses.
/// Anything unknown falls through to the default 500 handler (details are never leaked).
/// </summary>
internal sealed class ApiExceptionHandler(IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException v => new ValidationProblemDetails(
                v.Errors.GroupBy(e => e.PropertyName)
                        .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray()))
            { Status = StatusCodes.Status400BadRequest, Title = "Validation failed." },
            DomainException d => new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Domain rule violated.", Detail = d.Message },
            NotFoundException n => new ProblemDetails { Status = StatusCodes.Status404NotFound, Title = "Not found.", Detail = n.Message },
            InvalidCredentialsException c => new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Unauthorized.", Detail = c.Message },
            TooManyAttemptsException t => new ProblemDetails { Status = StatusCodes.Status429TooManyRequests, Title = "Too many attempts.", Detail = t.Message },
            _ => null,
        };

        if (problem is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        if (exception is TooManyAttemptsException)
        {
            httpContext.Response.Headers.RetryAfter = ((int)AttemptGuard.Window.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }
}
