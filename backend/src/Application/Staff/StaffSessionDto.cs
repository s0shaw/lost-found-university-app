namespace UniversityLostFound.Application.Staff;

// No token field: it stays in the httpOnly cookie, never in a body a script could read.
public sealed record StaffSessionDto(string UniversityId, string FullName, DateTimeOffset ExpiresAt);
