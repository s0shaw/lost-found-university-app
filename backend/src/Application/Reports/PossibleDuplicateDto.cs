using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

// Public fields only: a duplicate warning must never turn into a channel for someone else's secrets.
public sealed record PossibleDuplicateDto(Guid Id, string Title, DateOnly OccurredOn, Guid LocationId)
{
    public static PossibleDuplicateDto From(ItemReport report)
        => new(report.Id, report.Title, report.OccurredOn, report.LocationId);
}
