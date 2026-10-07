using UniversityLostFound.Domain.Matching;
using UniversityLostFound.Domain.Reports;

namespace UniversityLostFound.UnitTests.Domain;

public sealed class MatchScorerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    private static readonly Guid Colour = Guid.NewGuid();
    private static readonly Guid Size = Guid.NewGuid();
    private static readonly Guid Mark = Guid.NewGuid();
    private static readonly Guid Cards = Guid.NewGuid();

    private static readonly Guid Black = Guid.NewGuid();
    private static readonly Guid Medium = Guid.NewGuid();
    private static readonly Guid NoMark = Guid.NewGuid();
    private static readonly Guid Sticker = Guid.NewGuid();
    private static readonly Guid HasCards = Guid.NewGuid();

    private static readonly SecretAnswer[] ReportAnswers =
    [
        new(Colour, Black), new(Size, Medium), new(Mark, NoMark), new(Cards, HasCards),
    ];

    private static readonly SecretAnswer[] ClaimAnswers =
    [
        new(Colour, Black), new(Size, Medium), new(Mark, Sticker), new(Cards, HasCards),
    ];

    private const string ReportText = "sol üst köşesinde çizik var, içinde mavi bir otobüs kartı vardı";
    private const string ClaimText = "içinde mavi bir otobüs kartı olmalı, deri cüzdan";

    private static readonly string[] WorkedExampleWords = ["mavi", "otobus", "karti"];
    private static readonly string[] StemWords = ["deri", "cuzdan", "kahverengi"];

    [Fact]
    public void The_worked_example_scores_65()
    {
        var breakdown = MatchScorer.ScoreClaim(Report(ReportAnswers, ReportText), ClaimAnswers, ClaimText, lostOn: null);

        Assert.Equal(65, breakdown.Score);
        Assert.Equal(3, breakdown.MatchedAnswers);
        Assert.Equal(4, breakdown.TotalAnswers);
        Assert.Equal(0.5, breakdown.TextOverlap, 3);
        Assert.Equal(WorkedExampleWords, breakdown.SharedWords);
        Assert.Null(breakdown.SameLocation);
    }

    [Fact]
    public void The_same_answers_and_the_same_text_score_100()
    {
        var breakdown = MatchScorer.ScoreClaim(Report(ReportAnswers, ReportText), ReportAnswers, ReportText, lostOn: null);

        Assert.Equal(100, breakdown.Score);
    }

    [Fact]
    public void Nothing_in_common_scores_0()
    {
        var wrongAnswers = new SecretAnswer[]
        {
            new(Colour, Medium), new(Size, Black), new(Mark, Sticker), new(Cards, NoMark),
        };

        var breakdown = MatchScorer.ScoreClaim(
            Report(ReportAnswers, ReportText), wrongAnswers, "kirmizi telefon kilifi", lostOn: null);

        Assert.Equal(0, breakdown.Score);
        Assert.Empty(breakdown.SharedWords);
    }

    [Fact]
    public void A_turkish_suffix_still_matches_the_stem()
    {
        var breakdown = MatchScorer.ScoreClaim(
            Report(ReportAnswers, "deri cüzdan kahverengi"), ReportAnswers, "cüzdanım kahverengiydi deriden", lostOn: null);

        Assert.Equal(StemWords, breakdown.SharedWords);
    }

    [Fact]
    public void Shared_stopwords_earn_nothing()
    {
        var breakdown = MatchScorer.ScoreClaim(
            Report(ReportAnswers, "bu ve bir ile için a an the"), ReportAnswers, "bu ve bir ile için a an the", lostOn: null);

        Assert.Empty(breakdown.SharedWords);
        Assert.Equal(0d, breakdown.TextOverlap, 3);
    }

    [Fact]
    public void An_empty_text_does_not_crash()
    {
        var breakdown = MatchScorer.ScoreClaim(Report(ReportAnswers, "..."), ReportAnswers, "!!!", lostOn: null);

        Assert.Equal(60, breakdown.Score);
        Assert.Empty(breakdown.SharedWords);
    }

    [Fact]
    public void An_unanswered_match_stays_in_the_denominator()
    {
        var breakdown = MatchScorer.ScoreClaim(
            Report(ReportAnswers, ReportText), ClaimAnswers.Take(2).ToList(), ReportText, lostOn: null);

        Assert.Equal(4, breakdown.TotalAnswers);
        Assert.Equal(2, breakdown.MatchedAnswers);
    }

    [Fact]
    public void One_matching_word_does_not_take_the_whole_text_component()
    {
        var breakdown = MatchScorer.ScoreClaim(
            Report(ReportAnswers, "cüzdan"), ReportAnswers, "cüzdan", lostOn: null);

        Assert.Equal(1d / 3, breakdown.TextOverlap, 3);
    }

    [Fact]
    public void A_full_match_with_the_location_component_is_exactly_100()
    {
        var location = Guid.NewGuid();
        var left = Report(ReportAnswers, ReportText, location);
        var right = Report(ReportAnswers, ReportText, location);

        var breakdown = MatchScorer.ScoreReports(left, right);

        Assert.Equal(100, breakdown.Score);
        Assert.True(breakdown.SameLocation);
    }

    [Fact]
    public void The_location_component_is_worth_at_most_ten_points()
    {
        var left = Report(ClaimAnswers, ClaimText, Guid.NewGuid());
        var same = Report(ReportAnswers, ReportText, left.LocationId);
        var elsewhere = Report(ReportAnswers, ReportText, Guid.NewGuid());

        var difference = MatchScorer.ScoreReports(left, same).Score - MatchScorer.ScoreReports(left, elsewhere).Score;

        Assert.InRange(difference, 1, 10);
    }

    [Fact]
    public void A_claim_that_lost_the_item_after_it_was_found_is_flagged()
    {
        var found = ItemReport.CreateFound(
            Guid.NewGuid(), Guid.NewGuid(), "Black wallet", "Left on a table.", Guid.NewGuid(),
            Today.AddDays(-5), ReportText, ReportAnswers, Guid.NewGuid(), Now);

        Assert.True(MatchScorer.ScoreClaim(found, ClaimAnswers, ClaimText, Today.AddDays(-1)).TimelineConflict);
        Assert.False(MatchScorer.ScoreClaim(found, ClaimAnswers, ClaimText, Today.AddDays(-6)).TimelineConflict);
        Assert.False(MatchScorer.ScoreClaim(found, ClaimAnswers, ClaimText, lostOn: null).TimelineConflict);
    }

    private static ItemReport Report(IEnumerable<SecretAnswer> answers, string secretDescription, Guid? locationId = null) =>
        ItemReport.CreateLost(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Black wallet",
            "Left it on a table.",
            locationId ?? Guid.NewGuid(),
            Today,
            secretDescription,
            answers,
            Now);
}
