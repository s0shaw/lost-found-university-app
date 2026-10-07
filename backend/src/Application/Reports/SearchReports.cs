using FluentValidation;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed class SearchReportsValidator : AbstractValidator<SearchReportsQuery>
{
    public SearchReportsValidator()
    {
        RuleFor(q => q.Type).IsInEnum();
        RuleFor(q => q.Q).MaximumLength(SearchReportsQuery.MaxTermLength);
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, SearchReportsQuery.MaxPageSize);
        RuleFor(q => q.To).GreaterThanOrEqualTo(q => q.From!.Value).When(q => q.From is not null && q.To is not null);
        RuleFor(q => q.Status)
            .Must(s => ItemReport.PublicStatuses.Contains(s!.Value))
            .When(q => q.Status is not null)
            .WithMessage($"Status must be one of: {string.Join(", ", ItemReport.PublicStatuses)}.");
    }
}

public sealed class SearchReportsHandler(IItemReportRepository reports)
{
    public Task<PagedResult<ItemReportSummaryDto>> HandleAsync(
        SearchReportsQuery query, CancellationToken cancellationToken = default)
        => reports.SearchPublicAsync(query, cancellationToken);
}
