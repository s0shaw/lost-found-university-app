using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.UniversityMembers;

public sealed class UniversityMember : Entity
{
    public const string CodePrefix = "UM";
    public const int FullNameMaxLength = 120;

    public string UniversityId { get; private set; }
    public string FullName { get; private set; }
    public UniversityMemberType Type { get; private set; }
    public DateTimeOffset? ValidFrom { get; private set; }
    public DateTimeOffset? ValidUntil { get; private set; }
    public bool IsActive { get; private set; }

    private UniversityMember(
        string universityId,
        string fullName,
        UniversityMemberType type,
        DateTimeOffset? validFrom,
        DateTimeOffset? validUntil
    )
    {
        UniversityId = universityId;
        FullName = fullName;
        Type = type;
        ValidFrom = validFrom;
        ValidUntil = validUntil;
        IsActive = true;
    }

    public static UniversityMember CreateMember(string universityId, string fullName) =>
        new(universityId, fullName, UniversityMemberType.Member, null, null);

    public static UniversityMember CreateVisitor(
        string universityId,
        string fullName,
        DateTimeOffset validFrom,
        DateTimeOffset validUntil
    )
    {
        if (validUntil <= validFrom)
        {
            throw new DomainException("Visitor validity must end after it starts.");
        }

        return new UniversityMember(universityId, fullName, UniversityMemberType.Visitor, validFrom, validUntil);
    }

    public bool IsValidAt(DateTimeOffset now) =>
        IsActive && (ValidFrom is null || now >= ValidFrom) && (ValidUntil is null || now <= ValidUntil);

    public void Deactivate() => IsActive = false;
}
