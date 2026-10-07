using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Infrastructure.Persistence.Repositories;

internal sealed class ItemReportRepository(AppDbContext db) : IItemReportRepository
{
    public async Task AddAsync(ItemReport report, CancellationToken cancellationToken = default)
        => await db.ItemReports.AddAsync(report, cancellationToken);

    public Task<ItemReport?> FindForClaimAsync(Guid id, CancellationToken cancellationToken = default)
        => db.ItemReports.Include(r => r.Claims).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task AddClaimAsync(Claim claim, CancellationToken cancellationToken = default)
        => await db.Claims.AddAsync(claim, cancellationToken);

    public async Task<IReadOnlyList<ItemReport>> ListForDuplicateCheckAsync(
        ItemReportType type, Guid categoryId, CancellationToken cancellationToken = default)
        => await db.ItemReports
            .AsNoTracking()
            .Where(r => r.Type == type
                && r.CategoryId == categoryId
                && (r.Status == ItemReportStatus.Open || r.Status == ItemReportStatus.PendingHandover))
            .ToListAsync(cancellationToken);

    public async Task<PagedResult<ItemReportSummaryDto>> SearchPublicAsync(
        SearchReportsQuery query, CancellationToken cancellationToken = default)
    {
        var reports = Filter(PublicReports(), query);
        var total = await reports.CountAsync(cancellationToken);

        var rows =
            from report in reports
            join category in db.Categories on report.CategoryId equals category.Id
            join location in db.UniversityLocations on report.LocationId equals location.Id
            orderby report.OccurredOn descending, report.Id
            select new ItemReportSummaryDto(
                report.Id, report.Type, report.Status, report.Title,
                category.Id, category.Name, location.Id, location.Name, report.OccurredOn);

        var items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ItemReportSummaryDto>(items, query.Page, query.PageSize, total);
    }

    public Task<ItemReportDetailDto?> FindPublicAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rows =
            from report in PublicReports()
            where report.Id == id
            join category in db.Categories on report.CategoryId equals category.Id
            join location in db.UniversityLocations on report.LocationId equals location.Id
            select new ItemReportDetailDto(
                report.Id, report.Type, report.Status, report.Title, report.PublicDescription,
                category.Id, category.Name, location.Id, location.Name, report.OccurredOn);

        return rows.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TrackedReportDto?> FindForOwnerAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default)
    {
        var report = await db.ItemReports
            .AsNoTracking()
            .FirstOrDefaultAsync(
                r => r.TrackingCode == trackingCode && r.ReporterUniversityMemberId == universityMemberId,
                cancellationToken);

        if (report is null)
        {
            return null;
        }

        var categoryName = await db.Categories
            .Where(c => c.Id == report.CategoryId)
            .Select(c => c.Name)
            .SingleAsync(cancellationToken);

        var locationNames = await db.UniversityLocations
            .Where(l => l.Id == report.LocationId || l.Id == report.HandoverPointId)
            .Select(l => new { l.Id, l.Name })
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

        return new TrackedReportDto(
            report.Id,
            report.TrackingCode,
            report.Type,
            report.Status,
            report.Title,
            report.PublicDescription,
            report.CategoryId,
            categoryName,
            report.LocationId,
            locationNames[report.LocationId],
            report.OccurredOn,
            report.SecretDescription,
            await ReadAnswersAsync(report, cancellationToken),
            report.HandoverPointId is { } handoverPointId ? locationNames[handoverPointId] : null,
            report.HandoverConfirmedAt,
            report.ReturnedAt,
            report.ClosedAt,
            report.CloseReason);
    }

    private async Task<IReadOnlyList<TrackedAnswerDto>> ReadAnswersAsync(
        ItemReport report, CancellationToken cancellationToken)
    {
        var questionIds = report.SecretAnswers.Select(a => a.QuestionId).ToList();
        var optionIds = report.SecretAnswers.Select(a => a.OptionId).ToList();

        var questions = await db.Questions
            .Where(q => questionIds.Contains(q.Id))
            .Select(q => new { q.Id, q.Text, q.DisplayOrder })
            .ToDictionaryAsync(q => q.Id, cancellationToken);

        var optionTexts = await db.QuestionOptions
            .Where(o => optionIds.Contains(o.Id))
            .Select(o => new { o.Id, o.Text })
            .ToDictionaryAsync(o => o.Id, o => o.Text, cancellationToken);

        // The answers are the record of what the reporter said. An edited question bank can cost
        // them their wording, never the answer itself, so the list is driven by the answers.
        return report.SecretAnswers
            .OrderBy(a => questions.TryGetValue(a.QuestionId, out var question)
                ? question.DisplayOrder
                : int.MaxValue)
            .Select(a => new TrackedAnswerDto(
                a.QuestionId,
                questions.GetValueOrDefault(a.QuestionId)?.Text,
                a.OptionId,
                optionTexts.GetValueOrDefault(a.OptionId)))
            .ToList();
    }

    public Task<ItemReport?> FindByClaimIdAsync(Guid claimId, CancellationToken cancellationToken = default)
        => db.ItemReports
            .Include(r => r.Claims)
            .FirstOrDefaultAsync(r => r.Claims.Any(c => c.Id == claimId), cancellationToken);

    public Task<ItemReport?> FindForOwnerActionAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default)
        => db.ItemReports
            .Include(r => r.Claims)
            .FirstOrDefaultAsync(
                r => r.TrackingCode == trackingCode && r.ReporterUniversityMemberId == universityMemberId,
                cancellationToken);

    public async Task<IReadOnlyList<ItemReport>> ListCandidatesAsync(
        ItemReportType oppositeType, Guid categoryId, CancellationToken cancellationToken = default)
        => await db.ItemReports
            .AsNoTracking()
            .Where(r => r.Type == oppositeType
                && r.CategoryId == categoryId
                && (r.Status == ItemReportStatus.Open || r.Status == ItemReportStatus.PendingHandover))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<ItemReport>> ListForDuplicateScanAsync(
        CancellationToken cancellationToken = default)
        => await db.ItemReports
            .AsNoTracking()
            .Where(r => r.Status == ItemReportStatus.Open || r.Status == ItemReportStatus.PendingHandover)
            .ToListAsync(cancellationToken);

    // Staff queries deliberately do not start from PublicReports(): the possession gate is the
    // public side's rule, and the whole point of these queues is what the public cannot see.
    public async Task<PagedResult<StaffReportSummaryDto>> SearchForStaffAsync(
        StaffReportsQuery query, CancellationToken cancellationToken = default)
    {
        var reports = query.Queue switch
        {
            StaffQueueType.PendingHandover => db.ItemReports.Where(r => r.Status == ItemReportStatus.PendingHandover),
            StaffQueueType.Stale => db.ItemReports.Where(r =>
                r.Status == ItemReportStatus.Open && r.OccurredOn < query.OccurredBefore!.Value),
            _ => db.ItemReports.Where(r => query.Ids!.Contains(r.Id)),
        };

        reports = reports.AsNoTracking();

        var total = await reports.CountAsync(cancellationToken);

        var rows =
            from report in reports
            join category in db.Categories on report.CategoryId equals category.Id
            join location in db.UniversityLocations on report.LocationId equals location.Id
            orderby report.OccurredOn, report.Id
            select new StaffReportSummaryDto(
                report.Id, report.TrackingCode, report.Type, report.Status, report.Title,
                category.Id, category.Name, location.Id, location.Name, report.OccurredOn,
                report.Claims.Count(c => c.Status == ClaimStatus.Pending));

        var items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StaffReportSummaryDto>(items, query.Page, query.PageSize, total);
    }

    public async Task<StaffItemReportDto?> FindForStaffAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var report = await db.ItemReports.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (report is null)
        {
            return null;
        }

        var categoryName = await db.Categories
            .Where(c => c.Id == report.CategoryId).Select(c => c.Name).SingleAsync(cancellationToken);

        var locationNames = await db.UniversityLocations
            .Where(l => l.Id == report.LocationId || l.Id == report.HandoverPointId)
            .ToDictionaryAsync(l => l.Id, l => l.Name, cancellationToken);

        var reporter = await db.UniversityMembers
            .Where(m => m.Id == report.ReporterUniversityMemberId)
            .Select(m => new { m.UniversityId, m.FullName })
            .SingleAsync(cancellationToken);

        return new StaffItemReportDto(
            report.Id, report.TrackingCode, report.Type, report.Status, report.Title, report.PublicDescription,
            report.CategoryId, categoryName, report.LocationId, locationNames[report.LocationId], report.OccurredOn,
            report.SecretDescription,
            await ReadAnswersAsync(report, cancellationToken),
            report.HandoverPointId is { } handoverPointId ? locationNames[handoverPointId] : null,
            reporter.UniversityId, reporter.FullName,
            report.HandoverConfirmedAt, report.ReturnedAt, report.ClosedAt, report.CloseReason);
    }

    public async Task<PagedResult<StaffClaimSummaryDto>> SearchClaimsForStaffAsync(
        StaffClaimsQuery query, CancellationToken cancellationToken = default)
    {
        var claims = db.Claims.AsNoTracking();

        if (query.Status is { } status)
        {
            claims = claims.Where(c => c.Status == status);
        }

        if (query.ReportId is { } reportId)
        {
            claims = claims.Where(c => c.ReportId == reportId);
        }

        var total = await claims.CountAsync(cancellationToken);

        // A claim carries no opened-at column, so the score orders the queue and the id breaks ties.
        var rows =
            from claim in claims
            join report in db.ItemReports on claim.ReportId equals report.Id
            join member in db.UniversityMembers on claim.ClaimantUniversityMemberId equals member.Id
            orderby claim.Score descending, claim.Id
            select new StaffClaimSummaryDto(
                claim.Id, claim.TrackingCode, report.Id, report.Title, report.Type,
                member.UniversityId, member.FullName, claim.Score, claim.Status, claim.Source,
                claim.LostOn, claim.DecidedAt);

        var items = await rows
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StaffClaimSummaryDto>(items, query.Page, query.PageSize, total);
    }

    private IQueryable<ItemReport> PublicReports()
        => db.ItemReports.AsNoTracking().Where(r => ItemReport.PublicStatuses.Contains(r.Status));

    private static IQueryable<ItemReport> Filter(IQueryable<ItemReport> reports, SearchReportsQuery query)
    {
        if (query.Type is not null)
        {
            reports = reports.Where(r => r.Type == query.Type);
        }

        if (query.Status is not null)
        {
            reports = reports.Where(r => r.Status == query.Status);
        }
        else
        {
            // A returned item is finished business. It stays reachable by its own link and under
            // ?status=Returned, but it does not sit between the reports still waiting for someone.
            reports = reports.Where(r => r.Status != ItemReportStatus.Returned);
        }

        if (query.Category is not null)
        {
            reports = reports.Where(r => r.CategoryId == query.Category);
        }

        if (query.Location is not null)
        {
            reports = reports.Where(r => r.LocationId == query.Location);
        }

        if (query.From is not null)
        {
            reports = reports.Where(r => r.OccurredOn >= query.From);
        }

        if (query.To is not null)
        {
            reports = reports.Where(r => r.OccurredOn <= query.To);
        }

        var term = query.Q?.Trim().ToLowerInvariant();

        if (!string.IsNullOrEmpty(term))
        {
            // EF translates ToLower() to SQL lower(); the culture-aware overloads CA1304/CA1311/CA1862
            // ask for have no SQL translation.
#pragma warning disable CA1304, CA1311, CA1862
            reports = reports.Where(r =>
                r.Title.ToLower().Contains(term) || r.PublicDescription.ToLower().Contains(term));
#pragma warning restore CA1304, CA1311, CA1862
        }

        return reports;
    }
}
