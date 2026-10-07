using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public enum StaffQueueType
{
    PendingHandover = 0,
    Stale = 1,
    PossibleDuplicate = 2,
}

// No background job: staleness is a question asked at read time.
public static class StaleReport
{
    public const int Days = 30;

    public static DateOnly CutoffFor(DateTimeOffset now) =>
        DateOnly.FromDateTime(now.UtcDateTime).AddDays(-Days);

    public static bool IsStale(ItemReportStatus status, DateOnly occurredOn, DateTimeOffset now) =>
        status == ItemReportStatus.Open && occurredOn < CutoffFor(now);
}
