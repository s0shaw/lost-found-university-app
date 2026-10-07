using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.UnitTests.Application;

internal sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow => now;
}

internal sealed class CountingUnitOfWork : IUnitOfWork
{
    public int SaveCalls { get; private set; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        SaveCalls++;
        return Task.FromResult(1);
    }
}

internal sealed class FakeMembers(params UniversityMember[] members) : IUniversityMemberRepository
{
    public Task<UniversityMember?> FindByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default)
        => Task.FromResult(members.FirstOrDefault(m => m.UniversityId == universityId));

    public Task<UniversityMember?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(members.FirstOrDefault(m => m.Id == id));
}

internal sealed class FakeReports : IItemReportRepository
{
    public List<ItemReport> Items { get; } = [];

    public Task AddAsync(ItemReport report, CancellationToken cancellationToken = default)
    {
        Items.Add(report);
        return Task.CompletedTask;
    }

    public List<Claim> AddedClaims { get; } = [];

    public Task<ItemReport?> FindForClaimAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(r => r.Id == id));

    public Task AddClaimAsync(Claim claim, CancellationToken cancellationToken = default)
    {
        AddedClaims.Add(claim);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<ItemReport>> ListForDuplicateCheckAsync(
        ItemReportType type, Guid categoryId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ItemReport>>(
            Items.Where(r => r.Type == type && r.CategoryId == categoryId).ToList());

    public Task<PagedResult<ItemReportSummaryDto>> SearchPublicAsync(
        SearchReportsQuery query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");

    public Task<ItemReportDetailDto?> FindPublicAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");

    public Task<TrackedReportDto?> FindForOwnerAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");

    public Task<ItemReport?> FindForOwnerActionAsync(
        string trackingCode, Guid universityMemberId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(
            r => r.TrackingCode == trackingCode && r.ReporterUniversityMemberId == universityMemberId));

    public Task<ItemReport?> FindByClaimIdAsync(Guid claimId, CancellationToken cancellationToken = default)
        => Task.FromResult(Items.FirstOrDefault(r => r.Claims.Any(c => c.Id == claimId)));

    public Task<IReadOnlyList<ItemReport>> ListCandidatesAsync(
        ItemReportType oppositeType, Guid categoryId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ItemReport>>(
            Items.Where(r => r.Type == oppositeType && r.CategoryId == categoryId).ToList());

    public Task<IReadOnlyList<ItemReport>> ListForDuplicateScanAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<ItemReport>>(Items);

    public Task<PagedResult<StaffReportSummaryDto>> SearchForStaffAsync(
        StaffReportsQuery query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");

    public Task<StaffItemReportDto?> FindForStaffAsync(Guid id, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");

    public Task<PagedResult<StaffClaimSummaryDto>> SearchClaimsForStaffAsync(
        StaffClaimsQuery query, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read.");
}

internal sealed class FakeCatalog(
    Guid categoryId,
    IReadOnlyDictionary<Guid, IReadOnlyList<Guid>> questionOptions,
    IReadOnlyList<UniversityLocation> locations) : ICatalogRepository
{
    public Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> GetQuestionOptionsAsync(
        Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(id == categoryId
            ? questionOptions
            : (IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>)new Dictionary<Guid, IReadOnlyList<Guid>>());

    public Task<UniversityLocation?> FindLocationAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(locations.FirstOrDefault(l => l.Id == id));

    public Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never list the catalog.");

    public Task<IReadOnlyList<LocationDto>> ListLocationsAsync(CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never list the catalog.");

    public Task<IReadOnlyDictionary<Guid, string>> GetQuestionTextsAsync(
        IReadOnlyCollection<Guid> questionIds, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read question texts.");

    public Task<IReadOnlyDictionary<Guid, string>> GetOptionTextsAsync(
        IReadOnlyCollection<Guid> optionIds, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Command tests never read option texts.");
}

internal sealed class FakeStaffAccounts(params StaffCredentials[] accounts) : IStaffAccountRepository
{
    public Task<StaffCredentials?> FindActiveByUniversityIdAsync(
        string universityId, CancellationToken cancellationToken = default)
        => Task.FromResult(accounts.FirstOrDefault(a => a.UniversityId == universityId));
}

internal sealed class FakePasswordHasher(string correctPassword) : IPasswordHasher
{
    public const string DummyHashValue = "dummy-hash";

    public int VerifyCalls { get; private set; }

    public string DummyHash => DummyHashValue;

    public static string HashOf(string password) => $"hash::{password}";

    public bool Verify(string password, string hash)
    {
        VerifyCalls++;
        return hash == HashOf(correctPassword) && password == correctPassword;
    }
}

internal sealed class FakeTokenIssuer(DateTimeOffset now) : IStaffTokenIssuer
{
    public StaffToken Issue(Guid staffAccountId, string universityId, string fullName)
        => new($"token-for-{staffAccountId}", now.AddHours(8));
}
