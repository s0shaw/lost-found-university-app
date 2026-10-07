namespace UniversityLostFound.Application.Staff;

public sealed record StaffCredentials(Guid StaffAccountId, string UniversityId, string FullName, string PasswordHash);

public interface IStaffAccountRepository
{
    // Null for unknown id, inactive member, or inactive staff account — caller must not tell these apart.
    Task<StaffCredentials?> FindActiveByUniversityIdAsync(
        string universityId, CancellationToken cancellationToken = default);
}
