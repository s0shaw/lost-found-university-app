using FluentValidation;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Matching;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record CreateReportCommand(
    ItemReportType Type,
    string UniversityId,
    Guid CategoryId,
    string Title,
    string PublicDescription,
    Guid LocationId,
    DateOnly OccurredOn,
    string SecretDescription,
    IReadOnlyList<SecretAnswer> Answers,
    Guid? HandoverPointId);

public sealed class CreateReportValidator : AbstractValidator<CreateReportCommand>
{
    public CreateReportValidator()
    {
        RuleFor(c => c.Type).IsInEnum();
        RuleFor(c => c.UniversityId).NotEmpty();
        RuleFor(c => c.CategoryId).NotEmpty();
        RuleFor(c => c.Title).NotEmpty().MaximumLength(ItemReport.TitleMaxLength);
        RuleFor(c => c.PublicDescription).NotNull().MaximumLength(ItemReport.PublicDescriptionMaxLength);
        RuleFor(c => c.LocationId).NotEmpty();
        RuleFor(c => c.SecretDescription).NotEmpty().MaximumLength(ItemReport.SecretDescriptionMaxLength);
        RuleFor(c => c.Answers).NotEmpty();
        RuleForEach(c => c.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId).NotEmpty();
            a.RuleFor(x => x.OptionId).NotEmpty();
        });

        RuleFor(c => c.HandoverPointId).NotNull().When(c => c.Type == ItemReportType.Found);
        RuleFor(c => c.HandoverPointId).Null().When(c => c.Type == ItemReportType.Lost);
    }
}

public sealed class CreateReportHandler(
    IItemReportRepository reports,
    IUniversityMemberRepository members,
    ICatalogRepository catalog,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    private const int MaxDuplicateWarnings = 3;

    public async Task<CreatedReportDto> HandleAsync(
        CreateReportCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var reporter = await members.ResolveValidAsync(command.UniversityId, now, cancellationToken);

        var questionOptions = await catalog.GetQuestionOptionsAsync(command.CategoryId, cancellationToken);
        SecretAnswerRules.Validate(questionOptions, command.Answers);

        await catalog.RequireLocationAsync(command.LocationId, cancellationToken);

        var report = command.Type == ItemReportType.Found
            ? ItemReport.CreateFound(
                reporter.Id,
                command.CategoryId,
                command.Title,
                command.PublicDescription,
                command.LocationId,
                command.OccurredOn,
                command.SecretDescription,
                command.Answers,
                await catalog.RequireHandoverPointAsync(command.HandoverPointId!.Value, cancellationToken),
                now)
            : ItemReport.CreateLost(
                reporter.Id,
                command.CategoryId,
                command.Title,
                command.PublicDescription,
                command.LocationId,
                command.OccurredOn,
                command.SecretDescription,
                command.Answers,
                now);

        var possibleDuplicates = await FindPossibleDuplicatesAsync(report, cancellationToken);

        await reports.AddAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedReportDto.From(report, possibleDuplicates);
    }

    // A warning, never a block: a false positive would lock a real owner out of their own report.
    private async Task<IReadOnlyList<PossibleDuplicateDto>> FindPossibleDuplicatesAsync(
        ItemReport report, CancellationToken cancellationToken)
    {
        var candidates = await reports.ListForDuplicateCheckAsync(report.Type, report.CategoryId, cancellationToken);

        return candidates
            .Select(candidate => new { Report = candidate, MatchScorer.ScoreReports(report, candidate).Score })
            .Where(candidate => candidate.Score > MatchScorer.DuplicateThreshold)
            .OrderByDescending(candidate => candidate.Score)
            .Take(MaxDuplicateWarnings)
            .Select(candidate => PossibleDuplicateDto.From(candidate.Report))
            .ToList();
    }
}
