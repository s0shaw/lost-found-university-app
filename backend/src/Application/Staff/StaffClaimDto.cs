using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed record StaffClaimSummaryDto(
    Guid Id,
    string TrackingCode,
    Guid ReportId,
    string ReportTitle,
    ItemReportType ReportType,
    string ClaimantUniversityId,
    string ClaimantFullName,
    int Score,
    ClaimStatus Status,
    ClaimSource Source,
    DateOnly? LostOn,
    DateTimeOffset? DecidedAt);

public sealed record StaffClaimComparisonDto(
    StaffClaimSummaryDto Claim,
    string ReportSecretDescription,
    string ClaimSecretDescription,
    IReadOnlyList<AnswerComparisonDto> Answers,
    int Score,
    int MatchedAnswers,
    int TotalAnswers,
    double TextOverlap,
    IReadOnlyList<string> SharedWords,
    bool? SameLocation,
    bool TimelineConflict,
    string? HandoverPointName);

public sealed record AnswerComparisonDto(
    Guid QuestionId, string? Question, string? ReportAnswer, string? ClaimAnswer, bool Matches);
