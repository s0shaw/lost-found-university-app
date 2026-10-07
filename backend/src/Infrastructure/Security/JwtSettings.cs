using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace UniversityLostFound.Infrastructure.Security;

public sealed record JwtSettings(string SigningKey, string Issuer, string Audience, int LifetimeHours)
{
    public const int MinimumKeyBytes = 32;
    public const string SectionName = "Jwt";

    public static JwtSettings Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var signingKey = section["SigningKey"] ?? string.Empty;

        // A short key is a silently weak signature, so this fails at startup rather than at runtime.
        if (Encoding.UTF8.GetByteCount(signingKey) < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"{SectionName}:SigningKey must be at least {MinimumKeyBytes} bytes. Set Jwt__SigningKey.");
        }

        return new JwtSettings(
            signingKey,
            section["Issuer"] ?? "universitylostfound",
            section["Audience"] ?? "universitylostfound",
            section.GetValue("LifetimeHours", 8));
    }

    public SymmetricSecurityKey Key => new(Encoding.UTF8.GetBytes(SigningKey));
}
