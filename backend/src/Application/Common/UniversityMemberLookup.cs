using UniversityLostFound.Application.UniversityMembers;
using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Application.Common;

public static class UniversityMemberLookup
{
    public const string RejectedMessage = "This university id is not valid.";

    public static async Task<UniversityMember> ResolveValidAsync(
        this IUniversityMemberRepository repository,
        string universityId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        var member = await repository.FindByUniversityIdAsync(Normalize(universityId), cancellationToken);

        if (member is null || !member.IsValidAt(now))
        {
            throw new DomainException(RejectedMessage);
        }

        return member;
    }

    // Short codes are printed on cards and typed by hand; the shift key is not part of the identity.
    public static string Normalize(string code) => code.Trim().ToUpperInvariant();
}
