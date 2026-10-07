using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class SeedTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Seed_fills_an_empty_database_once()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await DatabaseSeeder.SeedAsync(db, Now);
        await DatabaseSeeder.SeedAsync(db, Now);

        Assert.Equal(12, await db.UniversityLocations.CountAsync());
        Assert.Equal(3, await db.UniversityLocations.CountAsync(l => l.IsHandoverPoint));
        Assert.Equal(7, await db.Categories.CountAsync());
        Assert.Equal(27, await db.Questions.CountAsync());
        Assert.Equal(20, await db.UniversityMembers.CountAsync());
        Assert.Equal(2, await db.StaffAccounts.CountAsync());
        Assert.Equal(1, await db.ItemReports.CountAsync());

        var categories = await db.Categories.ToListAsync();
        Assert.All(categories, c => Assert.InRange(db.Questions.Count(q => q.CategoryId == c.Id), 3, 4));

        var visitors = await db.UniversityMembers.Where(m => m.Type == UniversityMemberType.Visitor).ToListAsync();
        Assert.Equal(3, visitors.Count(v => v.IsValidAt(Now)));
        Assert.Single(visitors, v => !v.IsValidAt(Now));

        var staff = await db.StaffAccounts.FirstAsync();
        Assert.True(BCrypt.Net.BCrypt.Verify("staffdemo123", staff.PasswordHash));

        var report = await db.ItemReports.Include(r => r.Claims).SingleAsync();
        Assert.Equal(ItemReportType.Found, report.Type);
        Assert.Equal(ItemReportStatus.Open, report.Status);
        Assert.True(report.IsPubliclyVisible);
        Assert.Equal(4, report.SecretAnswers.Count);
        Assert.Empty(report.Claims);
    }
}
