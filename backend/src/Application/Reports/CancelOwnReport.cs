using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Application.Reports;

public sealed record CancelOwnReportCommand(string UniversityId, string TrackingCode);

public sealed class CancelOwnReportValidator : AbstractValidator<CancelOwnReportCommand>
{
    public CancelOwnReportValidator()
    {
        RuleFor(c => c.UniversityId).NotEmpty().MaximumLength(ShortCode.LengthFor(UniversityMember.CodePrefix));
        RuleFor(c => c.TrackingCode).NotEmpty().MaximumLength(ShortCode.LengthFor(ItemReport.CodePrefix));
    }
}

public sealed class CancelOwnReportHandler(
    IItemReportRepository reports,
    IUniversityMemberRepository members,
    IUnitOfWork unitOfWork,
    IClock clock,
    AttemptGuard attempts)
{
    public async Task HandleAsync(CancelOwnReportCommand command, CancellationToken cancellationToken = default)
    {
        var attemptKey = TrackReportHandler.AttemptKey(command.UniversityId);
        attempts.EnsureAllowed(attemptKey, AttemptGuard.TrackLimit);

        var member = await members.FindByUniversityIdAsync(
            UniversityMemberLookup.Normalize(command.UniversityId), cancellationToken);
        var report = member is null
            ? null
            : await reports.FindForOwnerActionAsync(
                UniversityMemberLookup.Normalize(command.TrackingCode), member.Id, cancellationToken);

        if (report is null)
        {
            attempts.RecordFailure(attemptKey);
            throw new NotFoundException(TrackReportHandler.NotFoundMessage);
        }

        attempts.Reset(attemptKey);
        report.CancelByOwner(clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
