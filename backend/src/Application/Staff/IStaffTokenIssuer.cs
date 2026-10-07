namespace UniversityLostFound.Application.Staff;

public sealed record StaffToken(string Value, DateTimeOffset ExpiresAt);

public interface IStaffTokenIssuer
{
    StaffToken Issue(Guid staffAccountId, string universityId, string fullName);
}
