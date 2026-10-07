using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed class GetReportHandler(IItemReportRepository reports)
{
    public async Task<ItemReportDetailDto> HandleAsync(Guid id, CancellationToken cancellationToken = default)
        => await reports.FindPublicAsync(id, cancellationToken)
            ?? throw new NotFoundException(nameof(ItemReport), id);
}
