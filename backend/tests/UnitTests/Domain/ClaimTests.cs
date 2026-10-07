using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.UnitTests.Domain;

public sealed class ClaimTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 1, 1);
    private static readonly SecretAnswer[] Answers = [new(Guid.NewGuid(), Guid.NewGuid())];

    private static ItemReport Report() =>
        ItemReport.CreateLost(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Black backpack",
            "Left near the library entrance.",
            Guid.NewGuid(),
            OccurredOn,
            "Has a red keychain inside.",
            Answers,
            Now
        );

    private static Claim ClaimWith(ItemReport report, string secretDescription, SecretAnswer[] answers, int score) =>
        report.OpenClaim(Guid.NewGuid(), secretDescription, answers, OccurredOn, null, ClaimSource.User, score, Now);

    [Fact]
    public void A_new_claim_starts_pending_with_its_own_tracking_code()
    {
        var claim = ClaimWith(Report(), "Red keychain with a bus card.", Answers, 70);

        Assert.Equal(ClaimStatus.Pending, claim.Status);
        Assert.True(ShortCode.IsValid(claim.TrackingCode, Claim.CodePrefix));
        Assert.Null(claim.DecidedAt);
        Assert.Null(claim.DecidedByStaffId);
    }

    [Fact]
    public void A_claim_needs_a_secret_description_and_answers()
    {
        Assert.Throws<DomainException>(() => ClaimWith(Report(), "   ", Answers, 70));
        Assert.Throws<DomainException>(() => ClaimWith(Report(), "Red keychain.", [], 70));
        Assert.Throws<DomainException>(() => ClaimWith(Report(), "Red keychain.", null!, 70));
    }

    [Fact]
    public void A_claim_cannot_answer_the_same_question_twice()
    {
        var questionId = Guid.NewGuid();
        SecretAnswer[] conflicting = [new(questionId, Guid.NewGuid()), new(questionId, Guid.NewGuid())];

        var ex = Assert.Throws<DomainException>(() => ClaimWith(Report(), "Red keychain.", conflicting, 70));

        Assert.Contains("answered twice", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_claim_cannot_be_dated_in_the_future()
    {
        var ex = Assert.Throws<DomainException>(() =>
            Report().OpenClaim(
                Guid.NewGuid(),
                "Red keychain.",
                Answers,
                OccurredOn.AddDays(1),
                null,
                ClaimSource.User,
                70,
                Now
            )
        );

        Assert.Contains("future", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public void Score_stays_between_zero_and_one_hundred(int score)
    {
        Assert.Throws<DomainException>(() => ClaimWith(Report(), "Red keychain.", Answers, score));
    }

    [Fact]
    public void A_decided_claim_cannot_be_decided_again()
    {
        var report = Report();
        var claim = ClaimWith(report, "Red keychain.", Answers, 70);
        report.RejectClaim(claim.Id, Guid.NewGuid(), "Answers did not match.", Now);

        Assert.Throws<DomainException>(() => report.ApproveClaim(claim.Id, Guid.NewGuid(), Now));
        Assert.Throws<DomainException>(() => report.WithdrawClaim(claim.Id, claim.ClaimantUniversityMemberId, Now));
    }

    [Fact]
    public void Rejecting_records_the_staff_note_and_decision()
    {
        var report = Report();
        var claim = ClaimWith(report, "Red keychain.", Answers, 70);
        var staff = Guid.NewGuid();

        report.RejectClaim(claim.Id, staff, "  Answers did not match.  ", Now);

        Assert.Equal(ClaimStatus.Rejected, claim.Status);
        Assert.Equal("Answers did not match.", claim.StaffNote);
        Assert.Equal(staff, claim.DecidedByStaffId);
        Assert.Equal(Now, claim.DecidedAt);
    }
}
