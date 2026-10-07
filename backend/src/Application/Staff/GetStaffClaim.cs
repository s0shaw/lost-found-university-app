using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Matching;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed class GetStaffClaimHandler(
    IItemReportRepository reports,
    IUniversityMemberRepository members,
    ICatalogRepository catalog)
{
    public async Task<StaffClaimComparisonDto> HandleAsync(
        Guid claimId, CancellationToken cancellationToken = default)
    {
        var report = await reports.FindByClaimIdAsync(claimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), claimId);

        var claim = report.Claims.Single(c => c.Id == claimId);

        // Recomputed, not stored: a second copy of a derived value would cost a migration for nothing.
        var breakdown = MatchScorer.ScoreClaim(report, claim.Answers, claim.SecretDescription, claim.LostOn);

        var claimant = await members.FindByIdAsync(claim.ClaimantUniversityMemberId, cancellationToken);

        var questionIds = report.SecretAnswers.Select(a => a.QuestionId)
            .Union(claim.Answers.Select(a => a.QuestionId)).ToList();
        var optionIds = report.SecretAnswers.Select(a => a.OptionId)
            .Union(claim.Answers.Select(a => a.OptionId)).ToList();

        var questions = await catalog.GetQuestionTextsAsync(questionIds, cancellationToken);
        var options = await catalog.GetOptionTextsAsync(optionIds, cancellationToken);

        // A found report carries its own handover point; a lost one only learns it from the claim,
        // and not before the claim is approved.
        var handoverPointId = report.HandoverPointId ?? claim.HandoverPointId;
        var handoverPoint = handoverPointId is { } id
            ? await catalog.FindLocationAsync(id, cancellationToken)
            : null;

        var summary = new StaffClaimSummaryDto(
            claim.Id, claim.TrackingCode, report.Id, report.Title, report.Type,
            claimant?.UniversityId ?? string.Empty, claimant?.FullName ?? string.Empty,
            claim.Score, claim.Status, claim.Source, claim.LostOn, claim.DecidedAt);

        return new StaffClaimComparisonDto(
            summary,
            report.SecretDescription,
            claim.SecretDescription,
            CompareAnswers(report, claim, questions, options),
            breakdown.Score,
            breakdown.MatchedAnswers,
            breakdown.TotalAnswers,
            breakdown.TextOverlap,
            breakdown.SharedWords,
            breakdown.SameLocation,
            breakdown.TimelineConflict,
            handoverPoint?.Name);
    }

    // Driven by the report's questions: an answer the claim never gave shows up as an empty cell.
    private static List<AnswerComparisonDto> CompareAnswers(
        ItemReport report,
        Claim claim,
        IReadOnlyDictionary<Guid, string> questions,
        IReadOnlyDictionary<Guid, string> options)
        => report.SecretAnswers
            .Select(reportAnswer =>
            {
                var claimAnswer = claim.Answers.FirstOrDefault(a => a.QuestionId == reportAnswer.QuestionId);

                return new AnswerComparisonDto(
                    reportAnswer.QuestionId,
                    questions.GetValueOrDefault(reportAnswer.QuestionId),
                    options.GetValueOrDefault(reportAnswer.OptionId),
                    claimAnswer is null ? null : options.GetValueOrDefault(claimAnswer.OptionId),
                    claimAnswer?.OptionId == reportAnswer.OptionId);
            })
            .ToList();
}
