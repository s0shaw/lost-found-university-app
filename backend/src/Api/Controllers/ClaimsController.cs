using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Claims;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Route("api/claims")]
[Produces("application/json")]
public sealed class ClaimsController : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<CreatedClaimDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CreatedClaimDto>> Create(
        [FromBody] CreateClaimCommand command,
        [FromServices] IValidator<CreateClaimCommand> validator,
        [FromServices] CreateClaimHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var created = await handler.HandleAsync(command, cancellationToken);

        // No Location header: reading a claim back is a staff action and that endpoint does not exist yet.
        return Created((Uri?)null, created);
    }
}
