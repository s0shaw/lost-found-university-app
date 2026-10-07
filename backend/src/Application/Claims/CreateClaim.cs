using FluentValidation;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Matching;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Claims;

public sealed record CreateClaimCommand(
    Guid ReportId,
    string UniversityId,
    string SecretDescription,
    IReadOnlyList<SecretAnswer> Answers,
    DateOnly? LostOn,
    Guid? HandoverPointId);

public sealed class CreateClaimValidator : AbstractValidator<CreateClaimCommand>
{
    public CreateClaimValidator()
    {
        RuleFor(c => c.ReportId).NotEmpty();
        RuleFor(c => c.UniversityId).NotEmpty();
        RuleFor(c => c.SecretDescription).NotEmpty().MaximumLength(Claim.SecretDescriptionMaxLength);
        RuleFor(c => c.Answers).NotEmpty();
        RuleForEach(c => c.Answers).ChildRules(a =>
        {
            a.RuleFor(x => x.QuestionId).NotEmpty();
            a.RuleFor(x => x.OptionId).NotEmpty();
        });
    }
}

public sealed class CreateClaimHandler(
    IItemReportRepository reports,
    IUniversityMemberRepository members,
    ICatalogRepository catalog,
    IUnitOfWork unitOfWork,
    IClock clock)
{
    public async Task<CreatedClaimDto> HandleAsync(
        CreateClaimCommand command, CancellationToken cancellationToken = default)
    {
        var now = clock.UtcNow;
        var claimant = await members.ResolveValidAsync(command.UniversityId, now, cancellationToken);

        var report = await reports.FindForClaimAsync(command.ReportId, cancellationToken)
            ?? throw new NotFoundException(nameof(ItemReport), command.ReportId);

        var questionOptions = await catalog.GetQuestionOptionsAsync(report.CategoryId, cancellationToken);
        SecretAnswerRules.Validate(questionOptions, command.Answers);

        var handoverPointId = await ResolveHandoverPointAsync(report, command.HandoverPointId, cancellationToken);
        var breakdown = MatchScorer.ScoreClaim(report, command.Answers, command.SecretDescription, command.LostOn);

        var claim = report.OpenClaim(
            claimant.Id,
            command.SecretDescription,
            command.Answers,
            command.LostOn,
            handoverPointId,
            ClaimSource.User,
            breakdown.Score,
            now);

        await reports.AddClaimAsync(claim, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return CreatedClaimDto.From(claim);
    }

    // Claiming a lost report means "I have it": the claimant picks where to drop it off.
    // On a found report the item is already on its way to the reporter's handover point.
    private async Task<Guid?> ResolveHandoverPointAsync(
        ItemReport report, Guid? handoverPointId, CancellationToken cancellationToken)
    {
        if (report.Type == ItemReportType.Found)
        {
            if (handoverPointId is not null)
            {
                throw new DomainException("A claim on a found report cannot pick a handover point.");
            }

            return null;
        }

        if (handoverPointId is null)
        {
            throw new DomainException("A claim on a lost report needs a handover point.");
        }

        return await catalog.RequireHandoverPointAsync(handoverPointId.Value, cancellationToken);
    }
}
