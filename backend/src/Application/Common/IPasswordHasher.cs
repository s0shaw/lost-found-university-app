namespace UniversityLostFound.Application.Common;

public interface IPasswordHasher
{
    bool Verify(string password, string hash);

    // Verified when no account matched, so an unknown university id takes as long as a wrong password.
    string DummyHash { get; }
}
