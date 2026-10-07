using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using UniversityLostFound.Infrastructure.Persistence;

namespace UniversityLostFound.IntegrationTests;

/// <summary>
/// Boots the real API in-process with an in-memory SQLite database, so
/// `dotnet test` needs no Docker and no PostgreSQL. The runtime still uses PostgreSQL.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string StaffUniversityId = "UM-204718";
    public const string StaffPassword = "staffdemo123";

    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", "unused-in-tests");
        builder.UseSetting("Database:MigrateOnStartup", "false");
        builder.UseSetting("Jwt:SigningKey", "integration-test-signing-key-32-bytes-long");

        // Every test request lands in the same rate limiting partition (TestServer has no remote ip),
        // so the default hourly permit would leak across unrelated tests. The limit gets its own test.
        builder.UseSetting("RateLimiting:PermitsPerHour", "10000");

        builder.ConfigureServices(services =>
        {
            // AddDbContext registers both the options and an IDbContextOptionsConfiguration (EF Core 9+);
            // both must go, otherwise Npgsql and SQLite end up registered together.
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();
            _connection.Open();
            services.AddDbContext<AppDbContext>(o => o.UseSqlite(_connection));

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _connection.Dispose();
        }
    }

    // https, not http: the staff cookie is Secure, and a CookieContainer won't send it back over http.
    public HttpClient CreateStaffClientAsync() => CreateClient(
        new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });

    public async Task<HttpClient> LoginAsStaffAsync()
    {
        var client = CreateStaffClientAsync();
        var response = await client.PostAsJsonAsync(
            "/api/auth/login", new { universityId = StaffUniversityId, password = StaffPassword });
        response.EnsureSuccessStatusCode();
        return client;
    }
}
