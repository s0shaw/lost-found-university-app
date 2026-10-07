using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record SearchReportsQuery
{
    public const int MaxPageSize = 100;
    public const int MaxTermLength = 100;

    public ItemReportType? Type { get; init; }
    public Guid? Category { get; init; }
    public Guid? Location { get; init; }
    public string? Q { get; init; }
    public DateOnly? From { get; init; }
    public DateOnly? To { get; init; }
    public ItemReportStatus? Status { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
