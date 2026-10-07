using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.UnitTests.Application;

public sealed class SearchReportsValidatorTests
{
    private readonly SearchReportsValidator _validator = new();

    [Fact]
    public void An_empty_query_is_valid_and_pages_from_the_first_page()
    {
        var query = new SearchReportsQuery();

        Assert.True(_validator.Validate(query).IsValid);
        Assert.Equal(1, query.Page);
        Assert.Equal(20, query.PageSize);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_page_below_one_is_rejected(int page)
        => Assert.False(_validator.Validate(new SearchReportsQuery { Page = page }).IsValid);

    [Theory]
    [InlineData(0)]
    [InlineData(SearchReportsQuery.MaxPageSize + 1)]
    public void A_page_size_outside_the_allowed_range_is_rejected(int pageSize)
        => Assert.False(_validator.Validate(new SearchReportsQuery { PageSize = pageSize }).IsValid);

    [Theory]
    [InlineData(ItemReportStatus.Open)]
    [InlineData(ItemReportStatus.Matched)]
    [InlineData(ItemReportStatus.Returned)]
    public void A_public_status_is_accepted(ItemReportStatus status)
        => Assert.True(_validator.Validate(new SearchReportsQuery { Status = status }).IsValid);

    [Theory]
    [InlineData(ItemReportStatus.PendingHandover)]
    [InlineData(ItemReportStatus.Closed)]
    [InlineData(ItemReportStatus.Cancelled)]
    public void A_status_the_public_side_never_shows_is_rejected(ItemReportStatus status)
        => Assert.False(_validator.Validate(new SearchReportsQuery { Status = status }).IsValid);

    [Fact]
    public void A_range_that_ends_before_it_starts_is_rejected()
    {
        var query = new SearchReportsQuery
        {
            From = new DateOnly(2026, 9, 10),
            To = new DateOnly(2026, 9, 1),
        };

        Assert.False(_validator.Validate(query).IsValid);
    }

    [Fact]
    public void An_open_ended_range_is_valid()
        => Assert.True(_validator.Validate(new SearchReportsQuery { From = new DateOnly(2026, 9, 10) }).IsValid);
}
