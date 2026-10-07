using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Application.Reports;

public sealed record TrackedReportDto(
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
    DateTimeOffset? HandoverConfirmedAt,
    DateTimeOffset? ReturnedAt,
    DateTimeOffset? ClosedAt,
    ReportCloseReason? CloseReason);

public sealed record TrackedAnswerDto(Guid QuestionId, string? Question, Guid OptionId, string? Answer);
