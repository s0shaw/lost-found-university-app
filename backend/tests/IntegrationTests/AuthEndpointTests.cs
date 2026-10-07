using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class AuthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly ApiFactory _factory = factory;

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await DatabaseSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(), Now, ApiFactory.StaffPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_correct_login_sets_an_httponly_cookie_and_keeps_the_token_out_of_the_body()
    {
        var client = _factory.CreateStaffClientAsync();

        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { universityId = ApiFactory.StaffUniversityId, password = ApiFactory.StaffPassword });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);

        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("eyJ", body, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("UM-000000", ApiFactory.StaffPassword)]           // unknown university id
    [InlineData(ApiFactory.StaffUniversityId, "wrong-password")]      // wrong password
    [InlineData("UM-482915", ApiFactory.StaffPassword)]           // a seeded member without a staff account
    public async Task Every_failing_login_answers_with_the_same_401_message(string universityId, string password)
    {
        var client = _factory.CreateStaffClientAsync();

        var message = await ReadProblemAsync(client, universityId, password);

        Assert.Equal(LoginHandler.InvalidCredentialsMessage, message);
    }

    [Fact]
    public async Task Me_needs_the_cookie()
    {
        var client = _factory.CreateStaffClientAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/auth/me")).StatusCode);

        var signedIn = await _factory.LoginAsStaffAsync();
        var session = await signedIn.GetFromJsonAsync<StaffSessionDto>("/api/auth/me");

        Assert.Equal(ApiFactory.StaffUniversityId, session!.UniversityId);
    }

    [Fact]
    public async Task Logout_expires_the_cookie()
    {
        var client = await _factory.LoginAsStaffAsync();

        var response = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Contains(
            "expires=thu, 01 jan 1970",
            Assert.Single(response.Headers.GetValues("Set-Cookie")).ToLowerInvariant(),
            StringComparison.Ordinal);
    }

    private static async Task<string?> ReadProblemAsync(HttpClient client, string universityId, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { universityId, password });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;
    }
}
