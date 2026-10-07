using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed record ApproveClaimCommand(Guid ClaimId, Guid StaffId);

public sealed record RejectClaimCommand(Guid ClaimId, Guid StaffId, string StaffNote);

public sealed class RejectClaimValidator : AbstractValidator<RejectClaimCommand>
{
    public RejectClaimValidator()
    {
        RuleFor(c => c.ClaimId).NotEmpty();

        // A rejection without a reason is a decision nobody can review later.
        RuleFor(c => c.StaffNote).NotEmpty().MaximumLength(Claim.StaffNoteMaxLength);
    }
}

public sealed class DecideClaimHandler(IItemReportRepository reports, IUnitOfWork unitOfWork, IClock clock)
{
    public Task ApproveAsync(ApproveClaimCommand command, CancellationToken cancellationToken = default)
        => DecideAsync(
            command.ClaimId,
            (report, now) => report.ApproveClaim(command.ClaimId, command.StaffId, now),
            cancellationToken);

    public Task RejectAsync(RejectClaimCommand command, CancellationToken cancellationToken = default)
        => DecideAsync(
            command.ClaimId,
            (report, now) => report.RejectClaim(command.ClaimId, command.StaffId, command.StaffNote, now),
            cancellationToken);

    private async Task DecideAsync(
        Guid claimId, Action<ItemReport, DateTimeOffset> decision, CancellationToken cancellationToken)
    {
        var report = await reports.FindByClaimIdAsync(claimId, cancellationToken)
            ?? throw new NotFoundException(nameof(Claim), claimId);

        decision(report, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
