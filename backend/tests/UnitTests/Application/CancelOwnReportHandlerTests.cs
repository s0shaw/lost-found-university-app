using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Application;

public sealed class CancelOwnReportHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private const string OwnerUniversityId = "UM-482915";
    private const string OtherUniversityId = "UM-916035";

    private static readonly Guid QuestionA = Guid.NewGuid();
    private static readonly Guid OptionA1 = Guid.NewGuid();

    [Fact]
    public async Task The_owner_cancels_and_the_pending_claims_are_rejected()
    {
        var world = new World();

        await world.Handler.HandleAsync(new CancelOwnReportCommand(OwnerUniversityId, world.Report.TrackingCode));

        Assert.Equal(ItemReportStatus.Cancelled, world.Report.Status);
        Assert.All(world.Report.Claims, c => Assert.Equal(ClaimStatus.Rejected, c.Status));
        Assert.Equal(1, world.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task A_lowercase_tracking_code_still_cancels()
    {
        var world = new World();

        await world.Handler.HandleAsync(
            new CancelOwnReportCommand(OwnerUniversityId, world.Report.TrackingCode.ToLowerInvariant()));

        Assert.Equal(ItemReportStatus.Cancelled, world.Report.Status);
    }

    [Theory]
    [InlineData("UM-000000")]      // unknown university id
    [InlineData(OtherUniversityId)]    // someone else's pair
    public async Task Every_failing_path_answers_like_the_tracking_endpoint(string universityId)
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<NotFoundException>(
            () => world.Handler.HandleAsync(new CancelOwnReportCommand(universityId, world.Report.TrackingCode)));

        Assert.Equal(TrackReportHandler.NotFoundMessage, error.Message);
    }

    [Fact]
    public async Task A_matched_report_is_no_longer_the_owners_to_withdraw()
    {
        var world = new World();
        world.Report.ApproveClaim(world.Report.Claims[0].Id, Guid.NewGuid(), world.Now);

        // Staff decided; the owner cannot undo that decision with a university id and a tracking code.
        await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(new CancelOwnReportCommand(OwnerUniversityId, world.Report.TrackingCode)));

        Assert.Equal(ItemReportStatus.Matched, world.Report.Status);
        Assert.Equal(ClaimStatus.Approved, world.Report.Claims[0].Status);
    }

    [Fact]
    public async Task A_returned_report_cannot_be_cancelled()
    {
        var world = new World();
        world.Report.ApproveClaim(world.Report.Claims[0].Id, Guid.NewGuid(), world.Now);
        world.Report.MarkReturned(world.Now);

        await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(new CancelOwnReportCommand(OwnerUniversityId, world.Report.TrackingCode)));
    }

    private sealed class World
    {
        public DateTimeOffset Now { get; } = CancelOwnReportHandlerTests.Now;
        public ItemReport Report { get; }
        public CountingUnitOfWork UnitOfWork { get; }
        public CancelOwnReportHandler Handler { get; }

        public World()
        {
            var owner = UniversityMember.CreateMember(OwnerUniversityId, "Elif Yalçın");
            var claimant = UniversityMember.CreateMember(OtherUniversityId, "Kerem Şahin");

            Report = ItemReport.CreateLost(
                owner.Id, Guid.NewGuid(), "Black wallet", "Left it on a table.", Guid.NewGuid(), Today,
                "Scratch on the corner.", [new SecretAnswer(QuestionA, OptionA1)], Now);

            Report.OpenClaim(
                claimant.Id, "Scratch on the corner.", [new SecretAnswer(QuestionA, OptionA1)],
                lostOn: null, handoverPointId: Guid.NewGuid(), ClaimSource.User, score: 90, Now);

            var reports = new FakeReports();
            reports.Items.Add(Report);

            UnitOfWork = new CountingUnitOfWork();
            Handler = new CancelOwnReportHandler(
                reports, new FakeMembers(owner, claimant), UnitOfWork, new FixedClock(Now),
                new AttemptGuard(new FixedClock(Now)));
        }
    }
}
