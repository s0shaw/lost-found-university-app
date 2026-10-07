using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Application.UniversityMembers;

public interface IUniversityMemberRepository
{
    Task<UniversityMember?> FindByUniversityIdAsync(string universityId, CancellationToken cancellationToken = default);

    Task<UniversityMember?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
