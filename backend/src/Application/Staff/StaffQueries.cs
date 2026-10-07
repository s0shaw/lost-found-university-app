using FluentValidation;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed record StaffReportsQuery
{
    public const int MaxPageSize = 100;

    public StaffQueueType Queue { get; init; } = StaffQueueType.PendingHandover;
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    // Set by the handler, never by the caller.
    public DateOnly? OccurredBefore { get; init; }
    public IReadOnlyList<Guid>? Ids { get; init; }
}

public sealed class StaffReportsQueryValidator : AbstractValidator<StaffReportsQuery>
{
    public StaffReportsQueryValidator()
    {
        RuleFor(q => q.Queue).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, StaffReportsQuery.MaxPageSize);
    }
}

public sealed record StaffClaimsQuery
{
    public const int MaxPageSize = 100;

    public ClaimStatus? Status { get; init; } = ClaimStatus.Pending;
    public Guid? ReportId { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public sealed class StaffClaimsQueryValidator : AbstractValidator<StaffClaimsQuery>
{
    public StaffClaimsQueryValidator()
    {
        RuleFor(q => q.Status).IsInEnum();
        RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, StaffClaimsQuery.MaxPageSize);
    }
}
