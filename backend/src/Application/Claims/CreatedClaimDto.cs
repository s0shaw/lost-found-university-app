using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Claims;

public sealed record CreatedClaimDto(Guid Id, string TrackingCode, ClaimStatus Status)
{
    // The score and its breakdown are staff-only; the claimant gets a tracking code and a status.
    public static CreatedClaimDto From(Claim claim) => new(claim.Id, claim.TrackingCode, claim.Status);
}
