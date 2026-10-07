using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Application.Staff;

namespace UniversityLostFound.Infrastructure.Persistence.Repositories;

internal sealed class StaffAccountRepository(AppDbContext db) : IStaffAccountRepository
{
    public Task<StaffCredentials?> FindActiveByUniversityIdAsync(
        string universityId, CancellationToken cancellationToken = default)
    {
        // The validity window (IsValidAt) is a visitor rule; a staff account does not expire.
        var rows =
            from account in db.StaffAccounts.AsNoTracking()
            join member in db.UniversityMembers on account.UniversityMemberId equals member.Id
            where member.UniversityId == universityId && member.IsActive && account.IsActive
            select new StaffCredentials(account.Id, member.UniversityId, member.FullName, account.PasswordHash);

        return rows.FirstOrDefaultAsync(cancellationToken);
    }
}
