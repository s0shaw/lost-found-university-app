using UniversityLostFound.Domain.Common;

namespace UniversityLostFound.Domain.Reports;

public sealed class Claim : Entity
{
    public const string CodePrefix = "CL";
    public const int SecretDescriptionMaxLength = 2000;
    public const int StaffNoteMaxLength = 1000;

    public const string ReportEndedNote = "The report was closed before this claim was decided.";

    private readonly List<SecretAnswer> _answers = [];

    private Claim()
    {
        TrackingCode = string.Empty;
        SecretDescription = string.Empty;
    }

    internal Claim(
        Guid reportId,
        string trackingCode,
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
        var normalizedDescription = secretDescription?.Trim() ?? string.Empty;

        if (normalizedDescription.Length == 0)
        {
            throw new DomainException("A claim needs a secret description.");
        }

        if (normalizedDescription.Length > SecretDescriptionMaxLength)
        {
            throw new DomainException($"Secret description cannot be longer than {SecretDescriptionMaxLength} characters.");
        }

        _answers.AddRange(answers ?? []);

        if (_answers.Count == 0)
        {
            throw new DomainException("A claim needs answers to the category questions.");
        }

        if (_answers.DistinctBy(a => a.QuestionId).Count() != _answers.Count)
        {
            throw new DomainException("A question cannot be answered twice.");
        }

        if (score is < 0 or > 100)
        {
            throw new DomainException("Score must be between 0 and 100.");
        }

        if (lostOn > DateOnly.FromDateTime(now.UtcDateTime))
        {
            throw new DomainException("A claim cannot be dated in the future.");
        }

        ReportId = reportId;
        TrackingCode = trackingCode;
        ClaimantUniversityMemberId = claimantUniversityMemberId;
        SecretDescription = normalizedDescription;
        LostOn = lostOn;
        HandoverPointId = handoverPointId;
        Source = source;
        Score = score;
        Status = ClaimStatus.Pending;
    }

    public Guid ReportId { get; private set; }
    public string TrackingCode { get; private set; }
    public Guid ClaimantUniversityMemberId { get; private set; }
    public string SecretDescription { get; private set; }
    public IReadOnlyList<SecretAnswer> Answers => _answers;
    public DateOnly? LostOn { get; private set; }
    public Guid? HandoverPointId { get; private set; }
    public ClaimSource Source { get; private set; }
    public int Score { get; private set; }
    public ClaimStatus Status { get; private set; }
    public string? StaffNote { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public Guid? DecidedByStaffId { get; private set; }

    internal void Approve(Guid staffId, DateTimeOffset now)
    {
        RequirePending();

        Status = ClaimStatus.Approved;
        DecidedByStaffId = staffId;
        DecidedAt = now;
    }

    internal void Reject(Guid? staffId, string staffNote, DateTimeOffset now)
    {
        RequirePending();

        var normalizedNote = staffNote?.Trim() ?? string.Empty;

        if (normalizedNote.Length == 0)
        {
            throw new DomainException("Rejecting a claim requires a staff note.");
        }

        if (normalizedNote.Length > StaffNoteMaxLength)
        {
            throw new DomainException($"Staff note cannot be longer than {StaffNoteMaxLength} characters.");
        }

        Status = ClaimStatus.Rejected;
        StaffNote = normalizedNote;
        DecidedByStaffId = staffId;
        DecidedAt = now;
    }

    internal void Withdraw(DateTimeOffset now)
    {
        RequirePending();

        Status = ClaimStatus.Withdrawn;
        DecidedAt = now;
    }

    private void RequirePending()
    {
        if (Status != ClaimStatus.Pending)
        {
            throw new DomainException("Only a pending claim can be decided.");
        }
    }
}
