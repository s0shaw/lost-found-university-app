using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class ReportsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions StringEnumJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private readonly ApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), Now);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Lost_report_is_created_and_becomes_readable_in_the_database()
    {
        var request = await LostRequestAsync();

        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();
        Assert.NotNull(created);
        Assert.StartsWith("LF-", created.TrackingCode, StringComparison.Ordinal);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var report = await db.ItemReports.SingleAsync(r => r.Id == created.Id);

        Assert.Equal(ItemReportStatus.Open, report.Status);
        Assert.Equal(3, report.SecretAnswers.Count);
    }

    [Fact]
    public async Task Found_report_is_created_pending_handover()
    {
        var request = await FoundRequestAsync();

        var response = await _client.PostAsJsonAsync("/api/reports", request);
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var report = await db.ItemReports.SingleAsync(r => r.Id == created!.Id);

        Assert.Equal(ItemReportStatus.PendingHandover, report.Status);
        Assert.False(report.IsPubliclyVisible);
    }

    [Fact]
    public async Task A_report_type_sent_as_a_string_is_accepted()
    {
        var body = JsonSerializer.Serialize(await LostRequestAsync(), StringEnumJson);

        Assert.Contains("\"type\":\"Lost\"", body, StringComparison.Ordinal);

        var response = await _client.PostAsync(
            "/api/reports", new StringContent(body, Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_university_id_gets_a_400_and_never_a_500()
    {
        var request = (await LostRequestAsync()) with { UniversityId = "UM-000000" };

        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("This university id is not valid.", problem?.Detail);
    }

    [Fact]
    public async Task A_missing_title_is_a_validation_problem()
    {
        var request = (await LostRequestAsync()) with { Title = "" };

        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateReportCommand.Title), problem!.Errors.Keys);
    }

    [Fact]
    public async Task A_found_report_dropped_off_at_a_plain_location_is_rejected()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var cafeteria = await db.UniversityLocations.FirstAsync(l => !l.IsHandoverPoint);
        var request = (await FoundRequestAsync()) with { HandoverPointId = cafeteria.Id };

        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Answering_only_one_question_of_the_category_is_rejected()
    {
        var request = await LostRequestAsync();
        var trimmed = request with { Answers = [request.Answers[0]] };

        var response = await _client.PostAsJsonAsync("/api/reports", trimmed);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_second_report_of_the_same_item_is_warned_about_without_leaking_secrets()
    {
        var first = await LostRequestAsync();
        var firstResponse = await _client.PostAsJsonAsync("/api/reports", first);
        var firstCreated = await firstResponse.Content.ReadFromJsonAsync<CreatedReportDto>();

        var secondResponse = await _client.PostAsJsonAsync(
            "/api/reports", first with { UniversityId = "UM-916035", Title = "Umbrella, black" });

        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var body = await secondResponse.Content.ReadAsStringAsync();
        var second = JsonSerializer.Deserialize<CreatedReportDto>(body, StringEnumJson);

        // Other tests in this class post the same report, so the warning may list more than one.
        var duplicate = Assert.Single(second!.PossibleDuplicates, d => d.Id == firstCreated!.Id);
        Assert.Equal(first.Title, duplicate.Title);
        Assert.DoesNotContain(first.SecretDescription, body, StringComparison.Ordinal);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(first.Answers[0].OptionId.ToString(), body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_report_describing_another_item_is_not_warned_about()
    {
        var request = (await LostRequestAsync()) with { SecretDescription = "kirmizi telefon kilifi cizikli" };

        var response = await _client.PostAsJsonAsync("/api/reports", request);
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        Assert.Empty(created!.PossibleDuplicates);
    }

    private async Task<CreateReportCommand> LostRequestAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = await db.Categories.FirstAsync(c => c.Name == "Other");
        var questions = await db.Questions.Where(q => q.CategoryId == category.Id).ToListAsync();
        var answers = new List<SecretAnswer>();

        foreach (var question in questions)
        {
            var option = await db.QuestionOptions.FirstAsync(o => o.QuestionId == question.Id);
            answers.Add(new SecretAnswer(question.Id, option.Id));
        }

        var location = await db.UniversityLocations.FirstAsync(l => !l.IsHandoverPoint);
        var member = await db.UniversityMembers.FirstAsync(m => m.UniversityId == "UM-204718");

        return new CreateReportCommand(
            ItemReportType.Lost,
            member.UniversityId,
            category.Id,
            "Black umbrella",
            "Left it by the window.",
            location.Id,
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1),
            "It has a wooden handle with initials.",
            answers,
            HandoverPointId: null);
    }

    private async Task<CreateReportCommand> FoundRequestAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.IsHandoverPoint);

        return (await LostRequestAsync()) with
        {
            Type = ItemReportType.Found,
            UniversityId = "UM-916035",
            HandoverPointId = handoverPoint.Id,
        };
    }
}
