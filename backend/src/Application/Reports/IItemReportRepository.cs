using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public interface IItemReportRepository
{
    Task AddAsync(ItemReport report, CancellationToken cancellationToken = default);

    // Tracked and with its claims loaded: the invariants of ItemReport read that list.
    Task<ItemReport?> FindForClaimAsync(Guid id, CancellationToken cancellationToken = default);

    // Claims carry an id from the moment the aggregate builds them, and EF reads a set key on an
    // entity reached through a navigation as "already stored". Without this the row is never inserted.
    Task AddClaimAsync(Claim claim, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemReport>> ListForDuplicateCheckAsync(
        ItemReportType type, Guid categoryId, CancellationToken cancellationToken = default);

    Task<PagedResult<ItemReportSummaryDto>> SearchPublicAsync(
        SearchReportsQuery query, CancellationToken cancellationToken = default);

    Task<ItemReportDetailDto?> FindPublicAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TrackedReportDto?> FindForOwnerAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default);

    // Tracked, with claims: the owner's own cancel goes through the same domain method as staff's.
    Task<ItemReport?> FindForOwnerActionAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default);

    // Tracked, with claims: the domain methods read that list.
    Task<ItemReport?> FindByClaimIdAsync(Guid claimId, CancellationToken cancellationToken = default);

    // Read models: no tracking, projected in sql.
    Task<PagedResult<StaffReportSummaryDto>> SearchForStaffAsync(
        StaffReportsQuery query, CancellationToken cancellationToken = default);

    Task<StaffItemReportDto?> FindForStaffAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<StaffClaimSummaryDto>> SearchClaimsForStaffAsync(
        StaffClaimsQuery query, CancellationToken cancellationToken = default);

    // Entities, because the scorer works on the aggregate.
    Task<IReadOnlyList<ItemReport>> ListCandidatesAsync(
        ItemReportType oppositeType, Guid categoryId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemReport>> ListForDuplicateScanAsync(CancellationToken cancellationToken = default);
}
