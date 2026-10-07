using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Matching;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed class GetStaffReportHandler(IItemReportRepository reports)
{
    public const int MaxCandidates = 5;

    public async Task<StaffReportDetailDto> HandleAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var detail = await reports.FindForStaffAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ItemReport), id);

        var claims = await reports.SearchClaimsForStaffAsync(
            new StaffClaimsQuery { ReportId = id, Status = null, PageSize = StaffClaimsQuery.MaxPageSize },
            cancellationToken);

        return new StaffReportDetailDto(detail, claims.Items, await CandidatesAsync(id, cancellationToken));
    }

    private async Task<IReadOnlyList<CandidateReportDto>> CandidatesAsync(
        Guid id, CancellationToken cancellationToken)
    {
        var report = await reports.FindForClaimAsync(id, cancellationToken);

        if (report is null)
        {
            return [];
        }

        var opposite = report.Type == ItemReportType.Lost ? ItemReportType.Found : ItemReportType.Lost;
        var candidates = await reports.ListCandidatesAsync(opposite, report.CategoryId, cancellationToken);

        return candidates
            .Select(c => new CandidateReportDto(
                c.Id, c.TrackingCode, c.Type, c.Title, c.OccurredOn, MatchScorer.ScoreReports(report, c).Score))
            .OrderByDescending(c => c.Score)
            .ThenBy(c => c.Id)
            .Take(MaxCandidates)
            .ToList();
    }
}
