using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Application;

public sealed class DecideClaimHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private static readonly Guid QuestionA = Guid.NewGuid();
    private static readonly Guid OptionA1 = Guid.NewGuid();

    [Fact]
    public async Task Approving_matches_the_report_and_rejects_the_rival_claims()
    {
        var world = new World();
        var winner = world.Report.Claims[0];
        var loser = world.Report.Claims[1];

        await world.Handler.ApproveAsync(new ApproveClaimCommand(winner.Id, world.StaffId));

        Assert.Equal(ItemReportStatus.Matched, world.Report.Status);
        Assert.Equal(ClaimStatus.Approved, winner.Status);
        Assert.Equal(ClaimStatus.Rejected, loser.Status);
        Assert.Equal(world.StaffId, winner.DecidedByStaffId);
        Assert.Equal(1, world.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Rejecting_records_the_note_and_leaves_the_report_open()
    {
        var world = new World();
        var claim = world.Report.Claims[0];

        await world.Handler.RejectAsync(new RejectClaimCommand(claim.Id, world.StaffId, "Answers do not match."));

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal("Answers do not match.", claim.StaffNote);
        Assert.Equal(ItemReportStatus.Open, world.Report.Status);
    }

    [Fact]
    public void A_rejection_without_a_note_never_reaches_the_handler()
    {
        var result = new RejectClaimValidator()
            .Validate(new RejectClaimCommand(Guid.NewGuid(), Guid.NewGuid(), string.Empty));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Approving_a_claim_on_a_closed_report_is_refused()
    {
        var world = new World();
        world.Report.Close(ReportCloseReason.Archived, world.Now);

        await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.ApproveAsync(new ApproveClaimCommand(world.Report.Claims[0].Id, world.StaffId)));
    }

    [Fact]
    public async Task An_unknown_claim_id_is_a_404()
    {
        var world = new World();

        await Assert.ThrowsAsync<UniversityLostFound.Application.Common.NotFoundException>(
            () => world.Handler.ApproveAsync(new ApproveClaimCommand(Guid.NewGuid(), world.StaffId)));
    }

    private sealed class World
    {
        public DateTimeOffset Now { get; } = DecideClaimHandlerTests.Now;
        public Guid StaffId { get; } = Guid.NewGuid();
        public ItemReport Report { get; }
        public FakeReports Reports { get; }
        public CountingUnitOfWork UnitOfWork { get; }
        public DecideClaimHandler Handler { get; }

        public World()
        {
            var reporter = UniversityMember.CreateMember("UM-482915", "Elif Yalçın");
            var winner = UniversityMember.CreateMember("UM-204718", "Derya Aksoy");
            var loser = UniversityMember.CreateMember("UM-916035", "Kerem Şahin");

            Report = ItemReport.CreateLost(
                reporter.Id, Guid.NewGuid(), "Black wallet", "Left it on a table.", Guid.NewGuid(), Today,
                "Scratch on the corner.", [new SecretAnswer(QuestionA, OptionA1)], Now);

            Report.OpenClaim(
                winner.Id, "Scratch on the corner.", [new SecretAnswer(QuestionA, OptionA1)],
                lostOn: null, handoverPointId: Guid.NewGuid(), ClaimSource.User, score: 90, Now);
            Report.OpenClaim(
                loser.Id, "No scratch.", [new SecretAnswer(QuestionA, OptionA1)],
                lostOn: null, handoverPointId: Guid.NewGuid(), ClaimSource.User, score: 40, Now);

            Reports = new FakeReports();
            Reports.Items.Add(Report);

            UnitOfWork = new CountingUnitOfWork();
            Handler = new DecideClaimHandler(Reports, UnitOfWork, new FixedClock(Now));
        }
    }
}
