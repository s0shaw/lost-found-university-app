using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed record ConfirmHandoverCommand(Guid ReportId);
public sealed record MarkReturnedCommand(Guid ReportId);
public sealed record CloseReportCommand(Guid ReportId, ReportCloseReason Reason);
public sealed record CancelReportCommand(Guid ReportId);

public sealed class CloseReportValidator : AbstractValidator<CloseReportCommand>
{
    public CloseReportValidator()
    {
        RuleFor(c => c.ReportId).NotEmpty();
        RuleFor(c => c.Reason).IsInEnum();
    }
}

public sealed class ReportActionsHandler(IItemReportRepository reports, IUnitOfWork unitOfWork, IClock clock)
{
    public Task ConfirmHandoverAsync(ConfirmHandoverCommand command, CancellationToken cancellationToken = default)
        => ApplyAsync(command.ReportId, (report, now) => report.ConfirmHandover(now), cancellationToken);

    public Task MarkReturnedAsync(MarkReturnedCommand command, CancellationToken cancellationToken = default)
        => ApplyAsync(command.ReportId, (report, now) => report.MarkReturned(now), cancellationToken);

    public Task CloseAsync(CloseReportCommand command, CancellationToken cancellationToken = default)
        => ApplyAsync(command.ReportId, (report, now) => report.Close(command.Reason, now), cancellationToken);

    public Task CancelAsync(CancelReportCommand command, CancellationToken cancellationToken = default)
        => ApplyAsync(command.ReportId, (report, now) => report.Cancel(now), cancellationToken);

    private async Task ApplyAsync(
        Guid reportId, Action<ItemReport, DateTimeOffset> transition, CancellationToken cancellationToken)
    {
        var report = await reports.FindForClaimAsync(reportId, cancellationToken)
            ?? throw new NotFoundException(nameof(ItemReport), reportId);

        transition(report, clock.UtcNow);

        await unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
