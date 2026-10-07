using UniversityLostFound.Domain.Common;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.UnitTests.Domain;

public sealed class ItemReportTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly OccurredOn = new(2026, 1, 1);
    private static readonly Guid Reporter = Guid.NewGuid();
    private static readonly Guid Staff = Guid.NewGuid();
    private static readonly SecretAnswer[] Answers = [new(Guid.NewGuid(), Guid.NewGuid())];

    private static ItemReport Lost() =>
        ItemReport.CreateLost(
            Reporter,
            Guid.NewGuid(),
            "Black backpack",
            "Left near the library entrance.",
            Guid.NewGuid(),
            OccurredOn,
            "Has a red keychain inside.",
            Answers,
            Now
        );

    private static ItemReport Found() =>
        ItemReport.CreateFound(
            Reporter,
            Guid.NewGuid(),
            "Black backpack",
            "Found near the library entrance.",
            Guid.NewGuid(),
            OccurredOn,
            "Has a red keychain inside.",
            Answers,
            Guid.NewGuid(),
            Now
        );

    private static Claim ClaimOn(ItemReport report, Guid claimant, int score = 70) =>
        report.OpenClaim(claimant, "Red keychain with a bus card.", Answers, OccurredOn, null, ClaimSource.User, score, Now);

    [Fact]
    public void Lost_report_opens_immediately_and_found_report_waits_for_handover()
    {
        Assert.Equal(ItemReportStatus.Open, Lost().Status);
        Assert.Equal(ItemReportStatus.PendingHandover, Found().Status);
    }

    [Fact]
    public void Report_waiting_for_handover_is_not_publicly_visible()
    {
        var report = Found();

        Assert.False(report.IsPubliclyVisible);

        report.ConfirmHandover(Now);

        Assert.True(report.IsPubliclyVisible);
        Assert.Equal(ItemReportStatus.Open, report.Status);
        Assert.Equal(Now, report.HandoverConfirmedAt);
    }

    [Fact]
    public void Tracking_code_is_generated_with_the_report_prefix()
    {
        Assert.True(ShortCode.IsValid(Lost().TrackingCode, ItemReport.CodePrefix));
    }

    [Fact]
    public void Claims_cannot_be_opened_before_the_item_is_handed_over()
    {
        var report = Found();

        var ex = Assert.Throws<DomainException>(() => ClaimOn(report, Guid.NewGuid()));

        Assert.Contains("open report", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Reporter_cannot_claim_their_own_report()
    {
        var report = Lost();

        var ex = Assert.Throws<DomainException>(() => ClaimOn(report, Reporter));

        Assert.Contains("cannot claim their own", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Same_university_member_cannot_claim_the_same_report_twice()
    {
        var report = Lost();
        var claimant = Guid.NewGuid();
        ClaimOn(report, claimant);

        var ex = Assert.Throws<DomainException>(() => ClaimOn(report, claimant));

        Assert.Contains("already claimed", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Approving_a_claim_matches_the_report_and_rejects_the_others()
    {
        var report = Lost();
        var winner = ClaimOn(report, Guid.NewGuid());
        var loser = ClaimOn(report, Guid.NewGuid());

        report.ApproveClaim(winner.Id, Staff, Now);

        Assert.Equal(ItemReportStatus.Matched, report.Status);
        Assert.Equal(ClaimStatus.Approved, winner.Status);
        Assert.Equal(ClaimStatus.Rejected, loser.Status);
        Assert.Equal(Staff, loser.DecidedByStaffId);
        Assert.NotNull(loser.StaffNote);
    }

    [Fact]
    public void Approving_a_claim_moves_its_handover_point_onto_a_lost_report()
    {
        var report = Lost();
        var handoverPoint = Guid.NewGuid();
        var winner = report.OpenClaim(
            Guid.NewGuid(), "Red keychain with a bus card.", Answers, OccurredOn, handoverPoint,
            ClaimSource.User, 70, Now);

        report.ApproveClaim(winner.Id, Staff, Now);

        Assert.Equal(handoverPoint, report.HandoverPointId);
    }

    [Fact]
    public void Withdrawn_claims_are_left_alone_when_another_claim_is_approved()
    {
        var report = Lost();
        var winner = ClaimOn(report, Guid.NewGuid());
        var withdrawn = ClaimOn(report, Guid.NewGuid());
        report.WithdrawClaim(withdrawn.Id, withdrawn.ClaimantUniversityMemberId, Now);

        report.ApproveClaim(winner.Id, Staff, Now);

        Assert.Equal(ClaimStatus.Withdrawn, withdrawn.Status);
    }

    [Fact]
    public void Rejecting_a_claim_requires_a_staff_note()
    {
        var report = Lost();
        var claim = ClaimOn(report, Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() => report.RejectClaim(claim.Id, Staff, "   ", Now));

        Assert.Contains("staff note", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Only_the_claimant_can_withdraw_their_claim()
    {
        var report = Lost();
        var claim = ClaimOn(report, Guid.NewGuid());

        var ex = Assert.Throws<DomainException>(() => report.WithdrawClaim(claim.Id, Guid.NewGuid(), Now));

        Assert.Contains("Only the claimant", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_claim_from_another_report_is_rejected()
    {
        var report = Lost();
        var other = ClaimOn(Lost(), Guid.NewGuid());

        Assert.Throws<DomainException>(() => report.ApproveClaim(other.Id, Staff, Now));
    }

    [Fact]
    public void Returning_requires_a_matched_report()
    {
        var report = Lost();

        Assert.Throws<DomainException>(() => report.MarkReturned(Now));

        var claim = ClaimOn(report, Guid.NewGuid());
        report.ApproveClaim(claim.Id, Staff, Now);
        report.MarkReturned(Now);

        Assert.Equal(ItemReportStatus.Returned, report.Status);
        Assert.Equal(Now, report.ReturnedAt);
    }

    [Theory]
    [InlineData(ItemReportStatus.Returned)]
    [InlineData(ItemReportStatus.Closed)]
    [InlineData(ItemReportStatus.Cancelled)]
    public void Finished_reports_accept_no_new_claims(ItemReportStatus status)
    {
        var report = Finished(status);

        var ex = Assert.Throws<DomainException>(() => ClaimOn(report, Guid.NewGuid()));

        Assert.Contains("open report", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(ItemReportStatus.Returned)]
    [InlineData(ItemReportStatus.Closed)]
    [InlineData(ItemReportStatus.Cancelled)]
    public void Finished_reports_cannot_be_closed_again(ItemReportStatus status)
    {
        var report = Finished(status);

        Assert.Throws<DomainException>(() => report.Close(ReportCloseReason.Archived, Now));
        Assert.Throws<DomainException>(() => report.Cancel(Now));
    }

    [Fact]
    public void Closing_a_report_rejects_the_claims_still_waiting()
    {
        var report = Lost();
        var pending = ClaimOn(report, Guid.NewGuid());

        report.Close(ReportCloseReason.Archived, Now);

        Assert.Equal(ClaimStatus.Rejected, pending.Status);
        Assert.Equal(Claim.ReportEndedNote, pending.StaffNote);
        Assert.Null(pending.DecidedByStaffId);
    }

    [Fact]
    public void Cancelling_a_report_rejects_the_claims_still_waiting()
    {
        var report = Lost();
        var pending = ClaimOn(report, Guid.NewGuid());

        report.Cancel(Now);

        Assert.Equal(ClaimStatus.Rejected, pending.Status);
    }

    [Fact]
    public void The_owner_cannot_withdraw_a_report_after_a_claim_was_approved()
    {
        var report = Lost();
        var approved = ClaimOn(report, Guid.NewGuid());
        report.ApproveClaim(approved.Id, Staff, Now);

        var ex = Assert.Throws<DomainException>(() => report.CancelByOwner(Now));

        Assert.Contains("staff", ex.Message, StringComparison.Ordinal);
        Assert.Equal(ItemReportStatus.Matched, report.Status);
        Assert.Equal(ClaimStatus.Approved, approved.Status);
    }

    [Fact]
    public void The_owner_can_still_withdraw_a_report_nobody_has_been_matched_to()
    {
        var report = Lost();
        var pending = ClaimOn(report, Guid.NewGuid());

        report.CancelByOwner(Now);

        Assert.Equal(ItemReportStatus.Cancelled, report.Status);
        Assert.Equal(ClaimStatus.Rejected, pending.Status);
    }

    [Fact]
    public void A_report_cannot_be_dated_in_the_future()
    {
        var ex = Assert.Throws<DomainException>(() =>
            ItemReport.CreateLost(
                Reporter,
                Guid.NewGuid(),
                "Black backpack",
                "Left near the library entrance.",
                Guid.NewGuid(),
                OccurredOn.AddDays(1),
                "Has a red keychain inside.",
                Answers,
                Now
            )
        );

        Assert.Contains("future", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_same_question_cannot_be_answered_twice()
    {
        var questionId = Guid.NewGuid();
        SecretAnswer[] conflicting = [new(questionId, Guid.NewGuid()), new(questionId, Guid.NewGuid())];

        var ex = Assert.Throws<DomainException>(() =>
            ItemReport.CreateLost(
                Reporter,
                Guid.NewGuid(),
                "Black backpack",
                "Left near the library entrance.",
                Guid.NewGuid(),
                OccurredOn,
                "Has a red keychain inside.",
                conflicting,
                Now
            )
        );

        Assert.Contains("answered twice", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Closing_records_the_reason()
    {
        var report = Lost();

        report.Close(ReportCloseReason.Donated, Now);

        Assert.Equal(ItemReportStatus.Closed, report.Status);
        Assert.Equal(ReportCloseReason.Donated, report.CloseReason);
        Assert.Equal(Now, report.ClosedAt);
    }

    private static ItemReport Finished(ItemReportStatus status)
    {
        var report = Lost();

        switch (status)
        {
            case ItemReportStatus.Returned:
                var claim = ClaimOn(report, Guid.NewGuid());
                report.ApproveClaim(claim.Id, Staff, Now);
                report.MarkReturned(Now);
                break;
            case ItemReportStatus.Closed:
                report.Close(ReportCloseReason.Archived, Now);
                break;
            default:
                report.Cancel(Now);
                break;
        }

        return report;
    }
}
