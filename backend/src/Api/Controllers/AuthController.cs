using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniversityLostFound.Api.Auth;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Api.Controllers;

[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public sealed class AuthController : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType<StaffSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<StaffSessionDto>> Login(
        [FromBody] LoginCommand command,
        [FromServices] IValidator<LoginCommand> validator,
        [FromServices] LoginHandler handler,
        CancellationToken cancellationToken)
    {
        await validator.ValidateAndThrowAsync(command, cancellationToken);
        var result = await handler.HandleAsync(command, cancellationToken);

        Response.Cookies.Append(StaffCookie.Name, result.Token.Value, StaffCookie.Options(result.Token.ExpiresAt));

        return Ok(result.Session);
    }

    // No server-side revocation — token is short lived, no refresh token, so clearing the cookie is enough.
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult Logout()
    {
        Response.Cookies.Append(StaffCookie.Name, string.Empty, StaffCookie.Expired());
        return NoContent();
    }

    // Cookie is httpOnly, so a page script can't read the session directly — it asks here instead.
    [HttpGet("me")]
    [Authorize(Roles = StaffClaims.StaffRole)]
    [ProducesResponseType<StaffSessionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<StaffSessionDto> Me() => Ok(User.Session());
}
