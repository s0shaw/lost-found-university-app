using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.UnitTests.Application;

public sealed class LoginHandlerTests
{
    private const string UniversityId = "UM-204718";
    private const string Password = "staffdemo123";

    [Fact]
    public async Task A_correct_password_issues_a_token_that_expires_in_the_future()
    {
        var world = new World();

        var result = await world.Handler.HandleAsync(new LoginCommand(UniversityId, Password));

        Assert.Equal(UniversityId, result.Session.UniversityId);
        Assert.Equal("Staff One", result.Session.FullName);
        Assert.True(result.Token.ExpiresAt > world.Now);
        Assert.Equal(result.Token.ExpiresAt, result.Session.ExpiresAt);
    }

    [Fact]
    public async Task A_lowercase_university_id_still_signs_in()
    {
        var world = new World();

        var result = await world.Handler.HandleAsync(new LoginCommand("um-204718", Password));

        Assert.Equal(UniversityId, result.Session.UniversityId);
    }

    [Theory]
    [InlineData("UM-000000", Password)]          // unknown university id
    [InlineData(UniversityId, "wrong-password")]     // wrong password
    [InlineData("UM-482915", Password)]          // a university member with no staff account
    public async Task Every_failing_path_answers_with_the_same_message(string universityId, string password)
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => world.Handler.HandleAsync(new LoginCommand(universityId, password)));

        Assert.Equal(LoginHandler.InvalidCredentialsMessage, error.Message);
    }

    [Fact]
    public async Task An_unknown_university_id_still_verifies_a_hash()
    {
        var world = new World();

        await Assert.ThrowsAsync<InvalidCredentialsException>(
            () => world.Handler.HandleAsync(new LoginCommand("UM-000000", Password)));

        // Returning before the hash check would make an unknown id measurably faster.
        Assert.Equal(1, world.Hasher.VerifyCalls);
    }

    private sealed class World
    {
        public DateTimeOffset Now { get; } = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

        public FakePasswordHasher Hasher { get; } = new(Password);

        public LoginHandler Handler { get; }

        public World()
        {
            var accounts = new FakeStaffAccounts(
                new StaffCredentials(Guid.NewGuid(), UniversityId, "Staff One", FakePasswordHasher.HashOf(Password)));

            Handler = new LoginHandler(accounts, Hasher, new FakeTokenIssuer(Now), new AttemptGuard(new FixedClock(Now)));
        }
    }
}
