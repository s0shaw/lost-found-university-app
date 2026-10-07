using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.UniversityMembers;

public sealed class StaffAccount : Entity
{
    public StaffAccount(Guid universityMemberId, string passwordHash)
    {
        UniversityMemberId = universityMemberId;
        PasswordHash = passwordHash;
        IsActive = true;
    }

    public Guid UniversityMemberId { get; private set; }
    public string PasswordHash { get; private set; }
    public bool IsActive { get; private set; }

    public void Deactivate() => IsActive = false;
}
