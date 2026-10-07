using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Reports;

namespace UniversityLostFound.Api.Controllers;

// POST, not GET: the university id and the tracking code are an identity pair, and a query string
// ends up in server logs, browser history and proxies.
[ApiController]
[Route("api/track")]
[Produces("application/json")]
public sealed class TrackController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<TrackedReportDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrackedReportDto>> Track(
        [FromBody] TrackReportCommand command,
        [FromServices] IValidator<TrackReportCommand> validator,
        [FromServices] TrackReportHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        return Ok(await handler.HandleAsync(command, cancellationToken));
    }

    // The owner's own withdrawal: same identity pair as tracking, so it lives on the same resource.
    [HttpPost("cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        [FromBody] CancelOwnReportCommand command,
        [FromServices] IValidator<CancelOwnReportCommand> validator,
        [FromServices] CancelOwnReportHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        await handler.HandleAsync(command, cancellationToken);
        return NoContent();
    }
}
