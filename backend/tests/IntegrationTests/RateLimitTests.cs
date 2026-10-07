using System.Net;

namespace UniversityLostFound.IntegrationTests;

public sealed class RateLimitTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    // TestServer sends no remote ip, so all of these requests share one partition — which is
    // what makes a two-request limit observable without sending the production sixty.
    private readonly HttpClient _client = factory
        .WithWebHostBuilder(builder => builder.UseSetting("RateLimiting:PermitsPerHour", "2"))
        .CreateClient();

    [Fact]
    public async Task A_caller_past_the_hourly_permit_gets_429_with_a_retry_after()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/locations")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/locations")).StatusCode);

        var rejected = await _client.GetAsync("/api/locations");

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.Equal(TimeSpan.FromHours(1), rejected.Headers.RetryAfter?.Delta);
    }

    [Fact]
    public async Task The_health_check_is_never_throttled()
    {
        for (var i = 0; i < 5; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/health")).StatusCode);
        }
    }
}
