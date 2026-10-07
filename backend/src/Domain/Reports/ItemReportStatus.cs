namespace UniversityLostFound.Domain.Reports;

public enum ItemReportStatus
{
    PendingHandover = 0,
    Open = 1,
    Matched = 2,
    Returned = 3,
    Closed = 4,
    Cancelled = 5,
}
