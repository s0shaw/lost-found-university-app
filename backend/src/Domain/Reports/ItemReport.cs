using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Reports;

public sealed class ItemReport : Entity
{
    public const string CodePrefix = "LF";
    public const int TitleMaxLength = 120;
    public const int PublicDescriptionMaxLength = 2000;
    public const int SecretDescriptionMaxLength = 2000;

    public static readonly IReadOnlyList<ItemReportStatus> PublicStatuses =
    [
        ItemReportStatus.Open,
        ItemReportStatus.Matched,
        ItemReportStatus.Returned,
    ];

    private readonly List<SecretAnswer> _secretAnswers = [];
    private readonly List<Claim> _claims = [];

    private ItemReport()
    {
        TrackingCode = string.Empty;
        Title = string.Empty;
        PublicDescription = string.Empty;
        SecretDescription = string.Empty;
    }

    private ItemReport(
        ItemReportType type,
        Guid reporterUniversityMemberId,
        Guid categoryId,
        string title,
        string publicDescription,
        Guid locationId,
        DateOnly occurredOn,
        string secretDescription,
        IEnumerable<SecretAnswer> secretAnswers,
        Guid? handoverPointId,
        DateTimeOffset now
    )
    {
        var normalizedTitle = title?.Trim() ?? string.Empty;
        var normalizedPublicDescription = publicDescription?.Trim() ?? string.Empty;
        var normalizedSecretDescription = secretDescription?.Trim() ?? string.Empty;

        if (normalizedTitle.Length == 0)
        {
            throw new DomainException("Title is required.");
        }

        if (normalizedTitle.Length > TitleMaxLength)
        {
            throw new DomainException($"Title cannot be longer than {TitleMaxLength} characters.");
        }

        if (normalizedPublicDescription.Length > PublicDescriptionMaxLength)
        {
            throw new DomainException($"Public description cannot be longer than {PublicDescriptionMaxLength} characters.");
        }

        if (normalizedSecretDescription.Length == 0)
        {
            throw new DomainException("A report needs a secret description.");
        }

        if (normalizedSecretDescription.Length > SecretDescriptionMaxLength)
        {
            throw new DomainException($"Secret description cannot be longer than {SecretDescriptionMaxLength} characters.");
        }

        if (occurredOn > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new DomainException("A report cannot be dated in the future.");
        }

        _secretAnswers.AddRange(secretAnswers ?? []);

        if (_secretAnswers.Count == 0)
        {
            throw new DomainException("A report needs answers to the category questions.");
        }

        if (_secretAnswers.DistinctBy(a => a.QuestionId).Count() != _secretAnswers.Count)
        {
            throw new DomainException("A question cannot be answered twice.");
        }

        TrackingCode = ShortCode.Generate(CodePrefix);
        Type = type;
        ReporterUniversityMemberId = reporterUniversityMemberId;
        CategoryId = categoryId;
        Title = normalizedTitle;
        PublicDescription = normalizedPublicDescription;
        LocationId = locationId;
        OccurredOn = occurredOn;
        SecretDescription = normalizedSecretDescription;
        HandoverPointId = handoverPointId;
        Status = type == ItemReportType.Found ? ItemReportStatus.PendingHandover : ItemReportStatus.Open;
    }

    public string TrackingCode { get; private set; }
    public ItemReportType Type { get; private set; }
    public Guid ReporterUniversityMemberId { get; private set; }
    public Guid CategoryId { get; private set; }
    public string Title { get; private set; }
    public string PublicDescription { get; private set; }
    public Guid LocationId { get; private set; }
    public DateOnly OccurredOn { get; private set; }
    public string SecretDescription { get; private set; }
    public IReadOnlyList<SecretAnswer> SecretAnswers => _secretAnswers;
    public Guid? HandoverPointId { get; private set; }
    public ItemReportStatus Status { get; private set; }
    public DateTimeOffset? HandoverConfirmedAt { get; private set; }
    public DateTimeOffset? ReturnedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public ReportCloseReason? CloseReason { get; private set; }
    public IReadOnlyList<Claim> Claims => _claims;

    public bool IsPubliclyVisible => PublicStatuses.Contains(Status);

    public static ItemReport CreateLost(
        Guid reporterUniversityMemberId,
        Guid categoryId,
        string title,
        string publicDescription,
        Guid locationId,
        DateOnly occurredOn,
        string secretDescription,
        IEnumerable<SecretAnswer> secretAnswers,
        DateTimeOffset now
    ) =>
        new(
            ItemReportType.Lost,
            reporterUniversityMemberId,
            categoryId,
            title,
            publicDescription,
            locationId,
            occurredOn,
            secretDescription,
            secretAnswers,
            handoverPointId: null,
            now
        );

    public static ItemReport CreateFound(
        Guid reporterUniversityMemberId,
        Guid categoryId,
        string title,
        string publicDescription,
        Guid locationId,
        DateOnly occurredOn,
        string secretDescription,
        IEnumerable<SecretAnswer> secretAnswers,
        Guid handoverPointId,
        DateTimeOffset now
    ) =>
        new(
            ItemReportType.Found,
            reporterUniversityMemberId,
            categoryId,
            title,
            publicDescription,
            locationId,
            occurredOn,
            secretDescription,
            secretAnswers,
            handoverPointId,
            now
        );

    public void ConfirmHandover(DateTimeOffset now)
    {
        if (Status != ItemReportStatus.PendingHandover)
        {
            throw new DomainException("Only a report waiting for handover can be confirmed.");
        }

        Status = ItemReportStatus.Open;
        HandoverConfirmedAt = now;
    }

    public Claim OpenClaim(
        Guid claimantUniversityMemberId,
        string secretDescription,
        IEnumerable<SecretAnswer> answers,
        DateOnly? lostOn,
        Guid? handoverPointId,
        ClaimSource source,
        int score,
        DateTimeOffset now
    )
    {
        if (Status != ItemReportStatus.Open)
        {
            throw new DomainException("Claims can only be opened on an open report.");
        }

        if (claimantUniversityMemberId == ReporterUniversityMemberId)
        {
            throw new DomainException("A reporter cannot claim their own report.");
        }

        if (_claims.Any(c => c.ClaimantUniversityMemberId == claimantUniversityMemberId))
        {
            throw new DomainException("This university member already claimed this report.");
        }

        var claim = new Claim(
            Id,
            ShortCode.Generate(Claim.CodePrefix),
            claimantUniversityMemberId,
            secretDescription,
            answers,
            lostOn,
            handoverPointId,
            source,
            score,
            now
        );

        _claims.Add(claim);
        return claim;
    }

    public void ApproveClaim(Guid claimId, Guid staffId, DateTimeOffset now)
    {
        if (Status != ItemReportStatus.Open)
        {
            throw new DomainException("Only an open report can be matched.");
        }

        var approved = FindClaim(claimId);
        approved.Approve(staffId, now);

        foreach (var other in _claims.Where(c => c.Id != claimId && c.Status == ClaimStatus.Pending))
        {
            other.Reject(staffId, "Another claim was approved for this report.", now);
        }

        // A lost report has no handover point of its own; the finder picks one when claiming, and the
        // owner can only read it off the report. A found report already has one and keeps it.
        HandoverPointId ??= approved.HandoverPointId;

        Status = ItemReportStatus.Matched;
    }

    public void RejectClaim(Guid claimId, Guid staffId, string staffNote, DateTimeOffset now) =>
        FindClaim(claimId).Reject(staffId, staffNote, now);

    public void WithdrawClaim(Guid claimId, Guid claimantUniversityMemberId, DateTimeOffset now)
    {
        var claim = FindClaim(claimId);

        if (claim.ClaimantUniversityMemberId != claimantUniversityMemberId)
        {
            throw new DomainException("Only the claimant can withdraw a claim.");
        }

        claim.Withdraw(now);
    }

    public void MarkReturned(DateTimeOffset now)
    {
        if (Status != ItemReportStatus.Matched)
        {
            throw new DomainException("Only a matched report can be returned.");
        }

        Status = ItemReportStatus.Returned;
        ReturnedAt = now;
    }

    public void Close(ReportCloseReason reason, DateTimeOffset now)
    {
        RequireNotFinished();

        RejectPendingClaims(now);
        Status = ItemReportStatus.Closed;
        CloseReason = reason;
        ClosedAt = now;
    }

    public void Cancel(DateTimeOffset now)
    {
        RequireNotFinished();

        RejectPendingClaims(now);
        Status = ItemReportStatus.Cancelled;
        ClosedAt = now;
    }

    public void CancelByOwner(DateTimeOffset now)
    {
        // A university id with a tracking code is not proof of identity, so it cannot undo a staff decision.
        if (Status == ItemReportStatus.Matched)
        {
            throw new DomainException("A matched report can only be closed by staff.");
        }

        Cancel(now);
    }

    private void RejectPendingClaims(DateTimeOffset now)
    {
        foreach (var claim in _claims.Where(c => c.Status == ClaimStatus.Pending))
        {
            claim.Reject(staffId: null, Claim.ReportEndedNote, now);
        }
    }

    private void RequireNotFinished()
    {
        if (Status is ItemReportStatus.Returned or ItemReportStatus.Closed or ItemReportStatus.Cancelled)
        {
            throw new DomainException("This report is already finished.");
        }
    }

    private Claim FindClaim(Guid claimId) =>
        _claims.SingleOrDefault(c => c.Id == claimId)
        ?? throw new DomainException("Claim does not belong to this report.");
}
