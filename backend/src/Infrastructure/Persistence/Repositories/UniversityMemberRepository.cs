using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Repositories;

internal sealed class UniversityMemberRepository(AppDbContext db) : IUniversityMemberRepository
{
    public Task<UniversityMember?> FindByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default)
        => db.UniversityMembers.AsNoTracking().FirstOrDefaultAsync(m => m.UniversityId == universityId, cancellationToken);

    public Task<UniversityMember?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => db.UniversityMembers.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
}
