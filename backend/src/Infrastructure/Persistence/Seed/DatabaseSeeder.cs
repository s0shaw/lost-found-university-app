using Microsoft.EntityFrameworkCore;
using UniversityLostFound.Domain.Categories;
using UniversityLostFound.Domain.Locations;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Domain.UniversityMembers;

namespace UniversityLostFound.Infrastructure.Persistence.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AppDbContext db,
        DateTimeOffset now,
        string? staffPassword = null,
        CancellationToken cancellationToken = default)
    {
        if (await db.UniversityLocations.AnyAsync(cancellationToken))
        {
            return;
        }

        var locations = SeedData.Locations
            .Select(l => new UniversityLocation(l.Name, l.IsHandoverPoint))
            .ToList();

        var categories = new List<Category>();
        var questions = new List<Question>();
        var options = new List<QuestionOption>();

        for (var i = 0; i < SeedData.Categories.Length; i++)
        {
            var (name, specificQuestion, specificOptions) = SeedData.Categories[i];
            var category = new Category(name, i + 1);
            categories.Add(category);

            AddQuestion(category, "What colour was it?", SeedData.ColourOptions, 1);
            AddQuestion(category, "How big was it?", SeedData.SizeOptions, 2);
            AddQuestion(category, "Any distinguishing mark?", SeedData.MarkOptions, 3);

            if (specificQuestion is not null)
            {
                AddQuestion(category, specificQuestion, specificOptions, 4);
            }
        }

        var members = SeedData.Members
            .Select(m => UniversityMember.CreateMember(m.UniversityId, m.FullName))
            .ToList();

        var visitors = SeedData.Visitors
            .Select(v => UniversityMember.CreateVisitor(
                v.UniversityId,
                v.FullName,
                now.AddDays(v.ValidFromDays),
                now.AddDays(v.ValidUntilDays)))
            .ToList();

        db.UniversityLocations.AddRange(locations);
        db.Categories.AddRange(categories);
        db.Questions.AddRange(questions);
        db.QuestionOptions.AddRange(options);
        var password = string.IsNullOrWhiteSpace(staffPassword) ? SeedData.StaffPassword : staffPassword;
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);   // hashed once, not once per account

        var staffAccounts = SeedData.StaffUniversityIds
            .Select(id => new StaffAccount(members.Single(m => m.UniversityId == id).Id, passwordHash))
            .ToList();

        db.UniversityMembers.AddRange(members);
        db.UniversityMembers.AddRange(visitors);
        db.StaffAccounts.AddRange(staffAccounts);
        db.ItemReports.Add(BuildSampleReport(locations, categories, questions, options, members, now));

        await db.SaveChangesAsync(cancellationToken);

        void AddQuestion(Category category, string text, string[] optionTexts, int order)
        {
            var question = new Question(category.Id, text, order);
            questions.Add(question);
            options.AddRange(optionTexts.Select((t, i) => new QuestionOption(question.Id, t, i + 1)));
        }
    }

    private static ItemReport BuildSampleReport(
        List<UniversityLocation> locations,
        List<Category> categories,
        List<Question> questions,
        List<QuestionOption> options,
        List<UniversityMember> members,
        DateTimeOffset now)
    {
        var wallet = categories.Single(c => c.Name == "Wallet & Cards");
        var walletQuestions = questions.Where(q => q.CategoryId == wallet.Id).OrderBy(q => q.DisplayOrder).ToList();

        var answers = new[]
        {
            Answer(walletQuestions[0], "Black"),
            Answer(walletQuestions[1], "Medium"),
            Answer(walletQuestions[2], "None"),
            Answer(walletQuestions[3], "Yes"),
        };

        var report = ItemReport.CreateFound(
            members.Single(m => m.UniversityId == "UM-482915").Id,
            wallet.Id,
            "Black leather wallet",
            "Left on a table in the cafeteria and handed in at reception.",
            locations.Single(l => l.Name == "Cafeteria").Id,
            DateOnly.FromDateTime(now.AddDays(-3).UtcDateTime),
            "scratch on the top-left corner, a blue transit card inside",
            answers,
            locations.Single(l => l.Name == "Main Reception").Id,
            now);

        report.ConfirmHandover(now.AddDays(-2));
        return report;

        SecretAnswer Answer(Question question, string optionText) =>
            new(question.Id, options.Single(o => o.QuestionId == question.Id && o.Text == optionText).Id);
    }
}
