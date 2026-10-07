using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record ItemReportDetailDto(
    Guid Id,
    ItemReportType Type,
    ItemReportStatus Status,
    string Title,
    string PublicDescription,
    Guid CategoryId,
    string CategoryName,
    Guid LocationId,
    string LocationName,
    DateOnly OccurredOn);
