using UniversityLostFound.Application.Claims;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Application;

public sealed class CreateClaimHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private static readonly Guid QuestionA = Guid.NewGuid();
    private static readonly Guid QuestionB = Guid.NewGuid();
    private static readonly Guid OptionA1 = Guid.NewGuid();
    private static readonly Guid OptionA2 = Guid.NewGuid();
    private static readonly Guid OptionB1 = Guid.NewGuid();

    private static readonly Guid CategoryId = Guid.NewGuid();

    private static readonly Dictionary<Guid, IReadOnlyList<Guid>> QuestionOptions = new()
    {
        [QuestionA] = [OptionA1, OptionA2],
        [QuestionB] = [OptionB1],
    };

    private const string ReportSecret = "Scratch on the top left corner, a blue bus card inside.";

    [Fact]
    public async Task A_claim_on_a_lost_report_is_stored_with_its_score()
    {
        var world = new World();

        var dto = await world.Handler.HandleAsync(Command(world, world.LostReport.Id));

        var claim = Assert.Single(world.LostReport.Claims);
        Assert.Equal(claim.Id, dto.Id);
        Assert.StartsWith("CL-", dto.TrackingCode, StringComparison.Ordinal);
        Assert.Equal(ClaimStatus.Pending, dto.Status);
        Assert.Equal(ClaimSource.User, claim.Source);
        Assert.Equal(world.Claimant.Id, claim.ClaimantUniversityMemberId);
        Assert.Equal(world.HandoverPoint.Id, claim.HandoverPointId);
        Assert.Equal(1, world.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task An_identical_claim_scores_100_and_the_score_is_written_to_the_record()
    {
        var world = new World();

        await world.Handler.HandleAsync(Command(world, world.LostReport.Id) with { SecretDescription = ReportSecret });

        Assert.Equal(100, Assert.Single(world.LostReport.Claims).Score);
    }

    [Fact]
    public async Task A_claim_that_agrees_on_nothing_scores_low()
    {
        var world = new World();
        var command = Command(world, world.LostReport.Id) with
        {
            SecretDescription = "kirmizi telefon kilifi",
            Answers = [new SecretAnswer(QuestionA, OptionA2), new SecretAnswer(QuestionB, OptionB1)],
        };

        await world.Handler.HandleAsync(command);

        Assert.Equal(30, Assert.Single(world.LostReport.Claims).Score);
    }

    [Fact]
    public async Task A_claim_on_a_found_report_uses_the_handover_point_of_the_report()
    {
        var world = new World();

        await world.Handler.HandleAsync(
            Command(world, world.FoundReport.Id) with { HandoverPointId = null, LostOn = Today.AddDays(-2) });

        var claim = Assert.Single(world.FoundReport.Claims);
        Assert.Null(claim.HandoverPointId);
        Assert.Equal(Today.AddDays(-2), claim.LostOn);
    }

    [Fact]
    public async Task A_claim_on_a_found_report_cannot_pick_a_handover_point()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, world.FoundReport.Id)));

        Assert.Equal("A claim on a found report cannot pick a handover point.", error.Message);
    }

    [Fact]
    public async Task A_claim_on_a_lost_report_needs_a_handover_point()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, world.LostReport.Id) with { HandoverPointId = null }));

        Assert.Equal("A claim on a lost report needs a handover point.", error.Message);
    }

    [Fact]
    public async Task A_handover_point_that_does_not_accept_handovers_is_rejected()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(
                Command(world, world.LostReport.Id) with { HandoverPointId = world.Cafeteria.Id }));

        Assert.Equal("This location does not accept handovers.", error.Message);
    }

    [Fact]
    public async Task A_reporter_cannot_claim_their_own_report()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, world.LostReport.Id) with { UniversityId = "UM-482915" }));

        Assert.Equal("A reporter cannot claim their own report.", error.Message);
    }

    [Fact]
    public async Task The_same_university_member_cannot_claim_the_same_report_twice()
    {
        var world = new World();
        await world.Handler.HandleAsync(Command(world, world.LostReport.Id));

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, world.LostReport.Id)));

        Assert.Equal("This university member already claimed this report.", error.Message);
    }

    [Fact]
    public async Task A_report_waiting_for_handover_cannot_be_claimed()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(
                Command(world, world.PendingReport.Id) with { HandoverPointId = null }));

        Assert.Equal("Claims can only be opened on an open report.", error.Message);
    }

    [Fact]
    public async Task An_unknown_report_is_a_not_found()
    {
        var world = new World();

        await Assert.ThrowsAsync<NotFoundException>(
            () => world.Handler.HandleAsync(Command(world, Guid.NewGuid())));
    }

    [Fact]
    public async Task An_unknown_university_id_is_rejected()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, world.LostReport.Id) with { UniversityId = "UM-000000" }));

        Assert.Equal(UniversityMemberLookup.RejectedMessage, error.Message);
    }

    [Fact]
    public async Task A_claim_answering_only_some_questions_is_rejected()
    {
        var world = new World();
        var command = Command(world, world.LostReport.Id) with
        {
            Answers = [new SecretAnswer(QuestionA, OptionA1)],
        };

        await Assert.ThrowsAsync<DomainException>(() => world.Handler.HandleAsync(command));
        Assert.Empty(world.LostReport.Claims);
    }

    private static CreateClaimCommand Command(World world, Guid reportId) =>
        new(
            reportId,
            "UM-204718",
            "It has a blue bus card inside.",
            [new SecretAnswer(QuestionA, OptionA1), new SecretAnswer(QuestionB, OptionB1)],
            LostOn: null,
            world.HandoverPoint.Id);

    private sealed class World
    {
        public World()
        {
            Reporter = UniversityMember.CreateMember("UM-482915", "Elif Yalçın");
            Claimant = UniversityMember.CreateMember("UM-204718", "Derya Aksoy");
            Cafeteria = new UniversityLocation("Cafeteria", isHandoverPoint: false);
            HandoverPoint = new UniversityLocation("Main Reception", isHandoverPoint: true);

            LostReport = ItemReport.CreateLost(
                Reporter.Id, CategoryId, "Black wallet", "Left it on a table.", Cafeteria.Id, Today,
                ReportSecret, ReportAnswers(), Now);

            FoundReport = ItemReport.CreateFound(
                Reporter.Id, CategoryId, "Black wallet", "Found on a table.", Cafeteria.Id, Today,
                ReportSecret, ReportAnswers(), HandoverPoint.Id, Now);
            FoundReport.ConfirmHandover(Now);

            PendingReport = ItemReport.CreateFound(
                Reporter.Id, CategoryId, "Blue umbrella", "Found by the door.", Cafeteria.Id, Today,
                "A wooden handle.", ReportAnswers(), HandoverPoint.Id, Now);

            Reports = new FakeReports();
            Reports.Items.AddRange([LostReport, FoundReport, PendingReport]);

            UnitOfWork = new CountingUnitOfWork();
            Handler = new CreateClaimHandler(
                Reports,
                new FakeMembers(Reporter, Claimant),
                new FakeCatalog(CategoryId, QuestionOptions, [Cafeteria, HandoverPoint]),
                UnitOfWork,
                new FixedClock(Now));
        }

        public UniversityMember Reporter { get; }
        public UniversityMember Claimant { get; }
        public UniversityLocation Cafeteria { get; }
        public UniversityLocation HandoverPoint { get; }
        public ItemReport LostReport { get; }
        public ItemReport FoundReport { get; }
        public ItemReport PendingReport { get; }
        public FakeReports Reports { get; }
        public CountingUnitOfWork UnitOfWork { get; }
        public CreateClaimHandler Handler { get; }

        private static SecretAnswer[] ReportAnswers() =>
            [new SecretAnswer(QuestionA, OptionA1), new SecretAnswer(QuestionB, OptionB1)];
    }
}
