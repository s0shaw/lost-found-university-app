using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record CreatedReportDto(
    Guid Id,
    string TrackingCode,
    IReadOnlyList<PossibleDuplicateDto> PossibleDuplicates)
{
    public static CreatedReportDto From(ItemReport report, IReadOnlyList<PossibleDuplicateDto> possibleDuplicates)
        => new(report.Id, report.TrackingCode, possibleDuplicates);
}
