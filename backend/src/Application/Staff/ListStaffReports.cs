using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Matching;

namespace UniversityLostFound.Application.Staff;

public sealed class ListStaffReportsHandler(IItemReportRepository reports, IClock clock)
{
    public async Task<PagedResult<StaffReportSummaryDto>> HandleAsync(
        StaffReportsQuery query, CancellationToken cancellationToken = default)
    {
        if (query.Queue != StaffQueueType.PossibleDuplicate)
        {
            return await reports.SearchForStaffAsync(
                query with { OccurredBefore = StaleReport.CutoffFor(clock.UtcNow) }, cancellationToken);
        }

        var partners = await FindDuplicatePartnersAsync(cancellationToken);

        var page = partners.Keys
            .OrderBy(id => id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        var rows = await reports.SearchForStaffAsync(query with { Ids = page, Page = 1 }, cancellationToken);

        return new PagedResult<StaffReportSummaryDto>(
            rows.Items.Select(r => r with { PossibleDuplicateIds = partners[r.Id] }).ToList(),
            query.Page,
            query.PageSize,
            partners.Count);
    }

    // Full scan, scored pairwise in memory. The dataset is three digits; move the scoring into sql
    // when it stops being.
    private async Task<Dictionary<Guid, List<Guid>>> FindDuplicatePartnersAsync(CancellationToken cancellationToken)
    {
        var live = await reports.ListForDuplicateScanAsync(cancellationToken);
        var partners = new Dictionary<Guid, List<Guid>>();

        foreach (var group in live.GroupBy(r => (r.Type, r.CategoryId)))
        {
            var items = group.ToList();

            for (var i = 0; i < items.Count; i++)
            {
                for (var j = i + 1; j < items.Count; j++)
                {
                    if (MatchScorer.ScoreReports(items[i], items[j]).Score <= MatchScorer.DuplicateThreshold)
                    {
                        continue;
                    }

                    Pair(partners, items[i].Id, items[j].Id);
                    Pair(partners, items[j].Id, items[i].Id);
                }
            }
        }

        return partners;
    }

    private static void Pair(Dictionary<Guid, List<Guid>> partners, Guid left, Guid right)
    {
        if (!partners.TryGetValue(left, out var list))
        {
            partners[left] = list = [];
        }

        list.Add(right);
    }
}
