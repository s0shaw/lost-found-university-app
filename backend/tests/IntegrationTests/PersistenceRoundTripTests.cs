using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Domain.Categories;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;
using UniversityLostFound.Infrastructure.Persistence;

namespace UniversityLostFound.IntegrationTests;

public sealed class PersistenceRoundTripTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Report_with_answers_and_a_claim_survives_a_round_trip()
    {
        var category = new Category("Wallet & Cards", 1);
        var question = new Question(category.Id, "Colour?", 1);
        var owner = UniversityMember.CreateMember("UM-482915", "Seed Owner");
        var claimant = UniversityMember.CreateMember("UM-730164", "Seed Claimant");
        var cafeteria = new UniversityLocation("Cafeteria", isHandoverPoint: false);
        var reception = new UniversityLocation("Main Reception", isHandoverPoint: true);

        var report = ItemReport.CreateFound(
            owner.Id,
            category.Id,
            "Black leather wallet",
            "Found on a table",
            cafeteria.Id,
            new DateOnly(2026, 9, 14),
            "scratch on the top-left corner",
            [new SecretAnswer(question.Id, Guid.NewGuid())],
            reception.Id,
            Now
        );

        report.ConfirmHandover(Now);
        report.OpenClaim(
            claimant.Id,
            "blue transit card inside",
            [new SecretAnswer(question.Id, Guid.NewGuid())],
            new DateOnly(2026, 9, 13),
            handoverPointId: null,
            ClaimSource.User,
            score: 65,
            Now
        );

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.AddRange(category, question, owner, claimant, cafeteria, reception);
            db.ItemReports.Add(report);
            await db.SaveChangesAsync();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var reloaded = await db.ItemReports
                .Include(r => r.Claims)
                .SingleAsync(r => r.Id == report.Id);

            Assert.Equal(report.TrackingCode, reloaded.TrackingCode);
            Assert.Equal(ItemReportStatus.Open, reloaded.Status);
            Assert.Equal(Now, reloaded.HandoverConfirmedAt);
            Assert.Equal(reception.Id, reloaded.HandoverPointId);
            Assert.Equal("scratch on the top-left corner", reloaded.SecretDescription);

            var answer = Assert.Single(reloaded.SecretAnswers);
            Assert.Equal(question.Id, answer.QuestionId);

            var claim = Assert.Single(reloaded.Claims);
            Assert.Equal(ClaimStatus.Pending, claim.Status);
            Assert.Equal(65, claim.Score);
            Assert.Equal(claimant.Id, claim.ClaimantUniversityMemberId);
            Assert.Single(claim.Answers);
        }
    }
}
