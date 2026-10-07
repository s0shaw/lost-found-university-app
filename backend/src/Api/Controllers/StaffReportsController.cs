using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffClaims.StaffRole)]
[Route("api/staff/reports")]
[Produces("application/json")]
public sealed class StaffReportsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<StaffReportSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<StaffReportSummaryDto>>> List(
        [FromQuery] StaffReportsQuery query,
        [FromServices] IValidator<StaffReportsQuery> validator,
        [FromServices] ListStaffReportsHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StaffReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StaffReportDetailDto>> GetById(
        Guid id,
        [FromServices] GetStaffReportHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost("{id:guid}/confirm-handover")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ConfirmHandover(
        Guid id,
        [FromServices] ReportActionsHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.ConfirmHandoverAsync(new ConfirmHandoverCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/mark-returned")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkReturned(
        Guid id,
        [FromServices] ReportActionsHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.MarkReturnedAsync(new MarkReturnedCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/close")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(
        Guid id,
        [FromBody] CloseReportRequest request,
        [FromServices] IValidator<CloseReportCommand> validator,
        [FromServices] ReportActionsHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new CloseReportCommand(id, request.Reason);
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        await handler.CloseAsync(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(
        Guid id,
        [FromServices] ReportActionsHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.CancelAsync(new CancelReportCommand(id), cancellationToken);
        return NoContent();
    }

    public sealed record CloseReportRequest(ReportCloseReason Reason);
}
