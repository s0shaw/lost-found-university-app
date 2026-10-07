using System.Text;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.Domain.Matching;

/// <summary>
/// Pure function.
/// The score ranks candidates for a human, it never decides anything on its own.
/// </summary>
public static class MatchScorer
{
    public const int AnswerWeight = 6;
    public const int TextWeight = 4;
    public const int LocationWeight = 1;

    public const int DuplicateThreshold = 80;

    private const int PrefixMatchLength = 4;
    private const int MinimumTextDenominator = 3;

    private static readonly HashSet<string> Stopwords = new(StringComparer.Ordinal)
    {
        "ve", "veya", "ile", "ama", "ancak", "icin", "gibi", "kadar", "daha", "cok", "az",
        "bir", "biri", "bu", "su", "da", "de", "ki", "mi", "ne", "her", "hic", "ise",
        "olan", "var", "yok", "icinde", "uzerinde", "yaninda", "ben", "bana", "bende", "benim",
        "once", "sonra", "a", "an", "the", "and", "or", "of", "in", "on", "at", "to", "for", "with",
        "is", "was", "be", "been", "it", "its", "my", "mine", "that", "this", "there",
        "has", "had", "have",
    };

    /// <summary>Claim against the report it was opened on. A claim has no location of its own.</summary>
    public static MatchBreakdown ScoreClaim(
        ItemReport report,
        IReadOnlyList<SecretAnswer> answers,
        string secretDescription,
        DateOnly? lostOn) =>
        Score(
            report.SecretAnswers,
            answers,
            report.SecretDescription,
            secretDescription,
            sameLocation: null,
            timelineConflict: report.Type == ItemReportType.Found && lostOn > report.OccurredOn);

    /// <summary>Report against report: duplicate detection and candidate suggestion.</summary>
    public static MatchBreakdown ScoreReports(ItemReport left, ItemReport right) =>
        Score(
            left.SecretAnswers,
            right.SecretAnswers,
            left.SecretDescription,
            right.SecretDescription,
            sameLocation: left.LocationId == right.LocationId,
            timelineConflict: false);

    private static MatchBreakdown Score(
        IReadOnlyList<SecretAnswer> leftAnswers,
        IReadOnlyList<SecretAnswer> rightAnswers,
        string leftText,
        string rightText,
        bool? sameLocation,
        bool timelineConflict)
    {
        var totalAnswers = Math.Max(leftAnswers.Count, rightAnswers.Count);
        var matchedAnswers = leftAnswers.Count(a =>
            rightAnswers.Any(b => b.QuestionId == a.QuestionId && b.OptionId == a.OptionId));
        var answerScore = totalAnswers == 0 ? 0 : (double)matchedAnswers / totalAnswers;

        var leftWords = Words(leftText);
        var rightWords = Words(rightText);
        var sharedWords = leftWords.Where(word => rightWords.Any(other => Matches(word, other))).ToList();

        // min() instead of Jaccard: whoever wrote less should not be punished for the other side's essay.
        var textScore = Math.Min(
            1.0,
            (double)sharedWords.Count / Math.Max(MinimumTextDenominator, Math.Min(leftWords.Count, rightWords.Count)));

        var weighted = (AnswerWeight * answerScore) + (TextWeight * textScore);
        var weights = AnswerWeight + TextWeight;

        if (sameLocation is { } locationMatches)
        {
            weighted += LocationWeight * (locationMatches ? 1 : 0);
            weights += LocationWeight;
        }

        return new MatchBreakdown(
            (int)Math.Round(100 * weighted / weights, MidpointRounding.AwayFromZero),
            matchedAnswers,
            totalAnswers,
            textScore,
            sharedWords,
            sameLocation,
            timelineConflict);
    }

    private static List<string> Words(string text) =>
        Fold(text)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => !Stopwords.Contains(word))
            .Distinct(StringComparer.Ordinal)
            .ToList();

    // Turkish is agglutinative: without a prefix match "cüzdanım" and "cüzdan" are two different words.
    private static bool Matches(string left, string right)
    {
        var shared = 0;

        while (shared < left.Length && shared < right.Length && left[shared] == right[shared])
        {
            shared++;
        }

        return (shared == left.Length && shared == right.Length) || shared >= PrefixMatchLength;
    }

    private static string Fold(string text)
    {
        var folded = new StringBuilder(text.Length);

        foreach (var character in text)
        {
            var lower = char.ToLowerInvariant(character);

            folded.Append(lower switch
            {
                'ı' or 'İ' => 'i',
                'ş' => 's',
                'ğ' => 'g',
                'ç' => 'c',
                'ö' => 'o',
                'ü' => 'u',
                _ => char.IsLetterOrDigit(lower) ? lower : ' ',
            });
        }

        return folded.ToString();
    }
}
