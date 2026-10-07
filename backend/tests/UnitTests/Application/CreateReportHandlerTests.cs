using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Application;

public sealed class CreateReportHandlerTests
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

    [Fact]
    public async Task Lost_report_opens_immediately_and_gets_a_tracking_code()
    {
        var world = new World();

        var dto = await world.Handler.HandleAsync(Command(world, ItemReportType.Lost));

        var report = Assert.Single(world.Reports.Items);
        Assert.Equal(ItemReportStatus.Open, report.Status);
        Assert.Equal(world.Member.Id, report.ReporterUniversityMemberId);
        Assert.Equal(report.TrackingCode, dto.TrackingCode);
        Assert.StartsWith("LF-", dto.TrackingCode, StringComparison.Ordinal);
        Assert.Equal(1, world.UnitOfWork.SaveCalls);
    }

    [Fact]
    public async Task Found_report_waits_for_the_item_to_arrive()
    {
        var world = new World();

        await world.Handler.HandleAsync(Command(world, ItemReportType.Found, world.HandoverPoint.Id));

        var report = Assert.Single(world.Reports.Items);
        Assert.Equal(ItemReportStatus.PendingHandover, report.Status);
        Assert.Equal(world.HandoverPoint.Id, report.HandoverPointId);
    }

    [Fact]
    public async Task Unknown_university_id_is_rejected()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, ItemReportType.Lost) with { UniversityId = "UM-000000" }));

        Assert.Equal(UniversityMemberLookup.RejectedMessage, error.Message);
        Assert.Empty(world.Reports.Items);
    }

    [Fact]
    public async Task Expired_visitor_is_rejected_with_the_same_message()
    {
        var world = new World(UniversityMember.CreateVisitor("UM-482915", "Lara Novak", Now.AddDays(-60), Now.AddDays(-30)));

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, ItemReportType.Lost)));

        Assert.Equal(UniversityMemberLookup.RejectedMessage, error.Message);
    }

    [Fact]
    public async Task A_report_answering_only_some_questions_is_rejected()
    {
        var world = new World();
        var command = Command(world, ItemReportType.Lost) with { Answers = [new SecretAnswer(QuestionA, OptionA1)] };

        await Assert.ThrowsAsync<DomainException>(() => world.Handler.HandleAsync(command));
        Assert.Empty(world.Reports.Items);
    }

    [Fact]
    public async Task An_option_from_another_question_is_rejected()
    {
        var world = new World();
        var command = Command(world, ItemReportType.Lost) with
        {
            Answers = [new SecretAnswer(QuestionA, OptionA1), new SecretAnswer(QuestionB, OptionA2)],
        };

        await Assert.ThrowsAsync<DomainException>(() => world.Handler.HandleAsync(command));
    }

    [Fact]
    public async Task An_unknown_category_is_rejected()
    {
        var world = new World();

        await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, ItemReportType.Lost) with { CategoryId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task A_location_that_does_not_exist_is_rejected()
    {
        var world = new World();

        await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, ItemReportType.Lost) with { LocationId = Guid.NewGuid() }));
    }

    [Fact]
    public async Task A_found_report_cannot_be_dropped_off_at_a_plain_location()
    {
        var world = new World();

        var error = await Assert.ThrowsAsync<DomainException>(
            () => world.Handler.HandleAsync(Command(world, ItemReportType.Found, world.Cafeteria.Id)));

        Assert.Equal("This location does not accept handovers.", error.Message);
    }

    [Fact]
    public void Validator_requires_a_handover_point_for_found_reports()
    {
        var result = new CreateReportValidator().Validate(Command(new World(), ItemReportType.Found));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReportCommand.HandoverPointId));
    }

    [Fact]
    public void Validator_rejects_a_handover_point_on_a_lost_report()
    {
        var result = new CreateReportValidator().Validate(Command(new World(), ItemReportType.Lost, Guid.NewGuid()));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateReportCommand.HandoverPointId));
    }

    [Fact]
    public async Task A_near_identical_report_of_the_same_type_comes_back_as_a_possible_duplicate()
    {
        var world = new World();
        var existing = Existing(world, ItemReportType.Lost, "Scratch on the top left corner.");
        world.Reports.Items.Add(existing);

        var dto = await world.Handler.HandleAsync(Command(world, ItemReportType.Lost));

        var duplicate = Assert.Single(dto.PossibleDuplicates);
        Assert.Equal(existing.Id, duplicate.Id);
        Assert.Equal(existing.Title, duplicate.Title);
        Assert.Equal(existing.LocationId, duplicate.LocationId);
    }

    [Fact]
    public async Task A_report_that_only_looks_alike_on_paper_is_not_flagged()
    {
        var world = new World();
        world.Reports.Items.Add(Existing(world, ItemReportType.Lost, "A red phone case with a cracked corner."));

        var dto = await world.Handler.HandleAsync(Command(world, ItemReportType.Lost));

        Assert.Empty(dto.PossibleDuplicates);
    }

    [Fact]
    public async Task The_opposite_report_type_is_never_a_duplicate()
    {
        var world = new World();
        world.Reports.Items.Add(Existing(world, ItemReportType.Found, "Scratch on the top left corner."));

        var dto = await world.Handler.HandleAsync(Command(world, ItemReportType.Lost));

        Assert.Empty(dto.PossibleDuplicates);
    }

    private static ItemReport Existing(World world, ItemReportType type, string secretDescription)
    {
        var answers = new[] { new SecretAnswer(QuestionA, OptionA1), new SecretAnswer(QuestionB, OptionB1) };

        return type == ItemReportType.Found
            ? ItemReport.CreateFound(
                Guid.NewGuid(), CategoryId, "Black wallet", "Left it on a table.", world.Cafeteria.Id, Today,
                secretDescription, answers, world.HandoverPoint.Id, Now)
            : ItemReport.CreateLost(
                Guid.NewGuid(), CategoryId, "Black wallet", "Left it on a table.", world.Cafeteria.Id, Today,
                secretDescription, answers, Now);
    }

    private static CreateReportCommand Command(World world, ItemReportType type, Guid? handoverPointId = null) =>
        new(
            type,
            "UM-482915",
            CategoryId,
            "Black wallet",
            "Left it on a table.",
            world.Cafeteria.Id,
            Today,
            "Scratch on the top left corner.",
            [new SecretAnswer(QuestionA, OptionA1), new SecretAnswer(QuestionB, OptionB1)],
            handoverPointId);

    private sealed class World
    {
        public World(UniversityMember? member = null)
        {
            Member = member ?? UniversityMember.CreateMember("UM-482915", "Elif Yalçın");
            Cafeteria = new UniversityLocation("Cafeteria", isHandoverPoint: false);
            HandoverPoint = new UniversityLocation("Main Reception", isHandoverPoint: true);

            Catalog = new FakeCatalog(CategoryId, QuestionOptions, [Cafeteria, HandoverPoint]);
            Members = new FakeMembers(Member);
            Reports = new FakeReports();
            UnitOfWork = new CountingUnitOfWork();
            Handler = new CreateReportHandler(Reports, Members, Catalog, UnitOfWork, new FixedClock(Now));
        }

        public UniversityMember Member { get; }
        public UniversityLocation Cafeteria { get; }
        public UniversityLocation HandoverPoint { get; }
        public FakeCatalog Catalog { get; }
        public FakeMembers Members { get; }
        public FakeReports Reports { get; }
        public CountingUnitOfWork UnitOfWork { get; }
        public CreateReportHandler Handler { get; }
    }
}
