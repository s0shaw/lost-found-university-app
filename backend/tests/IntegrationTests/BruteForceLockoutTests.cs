using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class BruteForceLockoutTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    public async Task InitializeAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await DatabaseSeeder.SeedAsync(
            scope.ServiceProvider.GetRequiredService<AppDbContext>(), Now, ApiFactory.StaffPassword);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Login_locks_after_repeated_failures_even_when_the_forwarded_ip_changes()
    {
        var client = factory.CreateStaffClientAsync();

        for (var i = 0; i < AttemptGuard.LoginLimit; i++)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
            {
                Content = JsonContent.Create(new { universityId = "UM-916035", password = "wrong-" + i }),
            };
            request.Headers.Add("X-Forwarded-For", $"203.0.113.{i}");
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.SendAsync(request)).StatusCode);
        }

        // The right password no longer helps until the window passes.
        var locked = await client.PostAsJsonAsync(
            "/api/auth/login", new { universityId = "UM-916035", password = ApiFactory.StaffPassword });

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
        Assert.NotNull(locked.Headers.RetryAfter);
    }

    [Fact]
    public async Task Tracking_locks_after_repeated_wrong_codes_for_one_university_id()
    {
        var client = factory.CreateClient();

        for (var i = 0; i < AttemptGuard.TrackLimit; i++)
        {
            var response = await client.PostAsJsonAsync(
                "/api/track", new { universityId = "UM-730164", trackingCode = $"LF-00000{i}" });
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        var locked = await client.PostAsJsonAsync(
            "/api/track", new { universityId = "UM-730164", trackingCode = "LF-000009" });

        Assert.Equal(HttpStatusCode.TooManyRequests, locked.StatusCode);
    }

    [Fact]
    public async Task The_staff_cookie_is_samesite_lax()
    {
        var response = await factory.CreateStaffClientAsync().PostAsJsonAsync(
            "/api/auth/login", new { universityId = ApiFactory.StaffUniversityId, password = ApiFactory.StaffPassword });

        Assert.Contains("samesite=lax", Assert.Single(response.Headers.GetValues("Set-Cookie")), StringComparison.OrdinalIgnoreCase);
    }
}
