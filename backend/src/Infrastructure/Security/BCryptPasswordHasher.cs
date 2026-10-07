using UniversityLostFound.Application.Common;

namespace UniversityLostFound.Infrastructure.Security;

internal sealed class BCryptPasswordHasher : IPasswordHasher
{
    public string DummyHash { get; } = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString());

    public bool Verify(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch (BCrypt.Net.SaltParseException)
        {
            // Unparseable hash = broken row, not a valid password.
            return false;
        }
    }
}
