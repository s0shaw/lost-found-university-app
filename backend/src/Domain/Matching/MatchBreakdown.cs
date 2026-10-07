namespace UniversityLostFound.Domain.Matching;

public sealed record MatchBreakdown(
    int Score,
    int MatchedAnswers,
    int TotalAnswers,
    double TextOverlap,
    IReadOnlyList<string> SharedWords,
    bool? SameLocation,
    bool TimelineConflict);
