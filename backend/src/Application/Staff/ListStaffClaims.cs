using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed class ListStaffClaimsHandler(IItemReportRepository reports)
{
    public Task<PagedResult<StaffClaimSummaryDto>> HandleAsync(
        StaffClaimsQuery query, CancellationToken cancellationToken = default)
        => reports.SearchClaimsForStaffAsync(query, cancellationToken);
}
