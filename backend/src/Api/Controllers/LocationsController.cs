using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Application.Catalog;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Route("api/locations")]
[Produces("application/json")]
public sealed class LocationsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<LocationDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<LocationDto>>> List(
        [FromServices] ListLocationsHandler handler, CancellationToken cancellationToken)
        => Ok(await handler.HandleAsync(cancellationToken));
}
