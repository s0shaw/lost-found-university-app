using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Infrastructure.Security;

internal sealed class JwtStaffTokenIssuer(JwtSettings settings, IClock clock) : IStaffTokenIssuer
{
    public StaffToken Issue(Guid staffAccountId, string universityId, string fullName)
    {
        var expiresAt = clock.UtcNow.AddHours(settings.LifetimeHours);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = settings.Issuer,
            Audience = settings.Audience,
            Expires = expiresAt.UtcDateTime,
            Claims = new Dictionary<string, object>
            {
                [JwtRegisteredClaimNames.Sub] = staffAccountId.ToString(),
                [StaffClaims.UniversityId] = universityId,
                [StaffClaims.FullName] = fullName,
                [StaffClaims.Role] = StaffClaims.StaffRole,
            },
            SigningCredentials = new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256),
        };

        return new StaffToken(new JsonWebTokenHandler().CreateToken(descriptor), expiresAt);
    }
}
