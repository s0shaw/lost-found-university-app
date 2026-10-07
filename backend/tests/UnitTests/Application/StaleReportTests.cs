using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.UnitTests.Application;

public sealed class StaleReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(29, false)]
    [InlineData(30, false)]   // the cutoff day itself is not stale yet
    [InlineData(31, true)]
    public void Staleness_turns_over_at_the_cutoff(int daysAgo, bool expected)
    {
        var occurredOn = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-daysAgo);

        Assert.Equal(expected, StaleReport.IsStale(ItemReportStatus.Open, occurredOn, Now));
    }

    [Theory]
    [InlineData(ItemReportStatus.Returned)]
    [InlineData(ItemReportStatus.Closed)]
    [InlineData(ItemReportStatus.Cancelled)]
    [InlineData(ItemReportStatus.Matched)]
    [InlineData(ItemReportStatus.PendingHandover)]
    public void A_report_that_is_no_longer_open_is_never_stale(ItemReportStatus status)
    {
        var longAgo = DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-365);

        Assert.False(StaleReport.IsStale(status, longAgo, Now));
    }
}
