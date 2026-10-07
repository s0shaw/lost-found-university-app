using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Route("api/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<PagedResult<ItemReportSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ItemReportSummaryDto>>> Search(
        [FromQuery] SearchReportsQuery query,
        [FromServices] IValidator<SearchReportsQuery> validator,
        [FromServices] SearchReportsHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(query, cancellationToken);
        return Ok(await handler.HandleAsync(query, cancellationToken));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ItemReportDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemReportDetailDto>> GetById(
        Guid id, [FromServices] GetReportHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(id, cancellationToken));

    [HttpPost]
    [ProducesResponseType<CreatedReportDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CreatedReportDto>> Create(
        [FromBody] CreateReportCommand command,
        [FromServices] IValidator<CreateReportCommand> validator,
        [FromServices] CreateReportHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var created = await handler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
