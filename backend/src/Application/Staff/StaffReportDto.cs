using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Staff;

public sealed record StaffReportSummaryDto(
    Guid Id,
    string TrackingCode,
    ItemReportType Type,
    ItemReportStatus Status,
    string Title,
    Guid CategoryId,
    string CategoryName,
    Guid LocationId,
    string LocationName,
    DateOnly OccurredOn,
    int PendingClaims)
{
    // Filled only by the duplicate queue; empty everywhere else.
    public IReadOnlyList<Guid> PossibleDuplicateIds { get; init; } = [];
}

// Staff see everything the public and owner views hide, including who reported it.
public sealed record StaffItemReportDto(
    Guid Id,
    string TrackingCode,
    ItemReportType Type,
    ItemReportStatus Status,
    string Title,
    string PublicDescription,
    Guid CategoryId,
    string CategoryName,
    Guid LocationId,
    string LocationName,
    DateOnly OccurredOn,
    string SecretDescription,
    IReadOnlyList<TrackedAnswerDto> Answers,
    string? HandoverPointName,
    string ReporterUniversityId,
    string ReporterFullName,
    DateTimeOffset? HandoverConfirmedAt,
    DateTimeOffset? ReturnedAt,
    DateTimeOffset? ClosedAt,
    ReportCloseReason? CloseReason);

public sealed record CandidateReportDto(
    Guid Id, string TrackingCode, ItemReportType Type, string Title, DateOnly OccurredOn, int Score);

public sealed record StaffReportDetailDto(
    StaffItemReportDto Report,
    IReadOnlyList<StaffClaimSummaryDto> Claims,
    IReadOnlyList<CandidateReportDto> Candidates);
