using System.Globalization;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Api.Auth;

internal static class StaffPrincipal
{
    // From the token, never the request body — trusting the caller's word would undo the auth check.
    public static Guid StaffId(this ClaimsPrincipal user) =>
        Guid.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    public static StaffSessionDto Session(this ClaimsPrincipal user) => new(
        user.FindFirstValue(StaffClaims.UniversityId)!,
        user.FindFirstValue(StaffClaims.FullName)!,
        DateTimeOffset.FromUnixTimeSeconds(
            long.Parse(user.FindFirstValue(JwtRegisteredClaimNames.Exp)!, CultureInfo.InvariantCulture)));
}
