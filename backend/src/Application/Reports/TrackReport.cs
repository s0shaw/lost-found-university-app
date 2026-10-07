using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Application.Reports;

public sealed record TrackReportCommand(string UniversityId, string TrackingCode);

public sealed class TrackReportValidator : AbstractValidator<TrackReportCommand>
{
    public TrackReportValidator()
    {
        RuleFor(c => c.UniversityId).NotEmpty().MaximumLength(ShortCode.LengthFor(UniversityMember.CodePrefix));
        RuleFor(c => c.TrackingCode).NotEmpty().MaximumLength(ShortCode.LengthFor(ItemReport.CodePrefix));
    }
}

public sealed class TrackReportHandler(
    IItemReportRepository reports, IUniversityMemberRepository members, AttemptGuard attempts)
{
    // An unknown university id, an unknown tracking code and someone else's pair all answer the same
    // way; a different answer would turn this into an oracle for either half.
    public const string NotFoundMessage = "No report matches this university id and tracking code.";

    public async Task<TrackedReportDto> HandleAsync(
        TrackReportCommand command, CancellationToken cancellationToken = default)
    {
        var attemptKey = AttemptKey(command.UniversityId);
        attempts.EnsureAllowed(attemptKey, AttemptGuard.TrackLimit);

        var member = await members.FindByUniversityIdAsync(
            UniversityMemberLookup.Normalize(command.UniversityId), cancellationToken);
        var report = member is null
            ? null
            : await reports.FindForOwnerAsync(
                UniversityMemberLookup.Normalize(command.TrackingCode), member.Id, cancellationToken);

        if (report is null)
        {
            attempts.RecordFailure(attemptKey);
            throw new NotFoundException(NotFoundMessage);
        }

        attempts.Reset(attemptKey);
        return report;
    }

    // Tracking and owner-cancel share one budget: both guess the same id + code pair.
    internal static string AttemptKey(string universityId) => "track:" + UniversityMemberLookup.Normalize(universityId);
}
