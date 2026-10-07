using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Api.Auth;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Authorize(Roles = StaffClaims.StaffRole)]
[Route("api/staff/claims")]
[Produces("application/json")]
public sealed class StaffClaimsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<StaffClaimSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PagedResult<StaffClaimSummaryDto>>> List(
        [FromQuery] StaffClaimsQuery query,
        [FromServices] IValidator<StaffClaimsQuery> validator,
        [FromServices] ListStaffClaimsHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<StaffClaimComparisonDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffClaimComparisonDto>> GetById(
        Guid id,
        [FromServices] GetStaffClaimHandler handler,
        CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost("{id:guid}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Approve(
        Guid id,
        [FromServices] DecideClaimHandler handler,
        CancellationToken cancellationToken)
    {
        // The staff id comes from the token, never from the body.
        await handler.ApproveAsync(new ApproveClaimCommand(id, User.StaffId()), cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reject(
        Guid id,
        [FromBody] RejectClaimRequest request,
        [FromServices] IValidator<RejectClaimCommand> validator,
        [FromServices] DecideClaimHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new RejectClaimCommand(id, User.StaffId(), request.StaffNote);
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        await handler.RejectAsync(command, cancellationToken);
        return NoContent();
    }

    public sealed record RejectClaimRequest(string StaffNote);
}
