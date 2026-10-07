using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record ItemReportSummaryDto(
    Guid Id,
    ItemReportType Type,
    ItemReportStatus Status,
    string Title,
    Guid CategoryId,
    string CategoryName,
    Guid LocationId,
    string LocationName,
    DateOnly OccurredOn);
