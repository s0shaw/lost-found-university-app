using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Claims;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class ClaimsEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions StringEnumJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private const string ReporterUniversityId = "UM-204718";
    private const string ClaimantUniversityId = "UM-916035";

    private readonly ApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), Now);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task A_claim_on_a_lost_report_is_created_and_carries_its_score()
    {
        var reportId = await CreateLostReportAsync();

        var response = await _client.PostAsJsonAsync("/api/claims", await ClaimAsync(reportId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CreatedClaimDto>(StringEnumJson);
        Assert.NotNull(created);
        Assert.StartsWith("CL-", created.TrackingCode, StringComparison.Ordinal);
        Assert.Equal(ClaimStatus.Pending, created.Status);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var claim = await db.Claims.SingleAsync(c => c.Id == created.Id);

        Assert.Equal(reportId, claim.ReportId);
        Assert.Equal(ClaimSource.User, claim.Source);
        Assert.InRange(claim.Score, 1, 100);
        Assert.Equal(4, claim.Answers.Count);
    }

    [Fact]
    public async Task The_score_never_reaches_the_claimant()
    {
        var reportId = await CreateLostReportAsync();

        var response = await _client.PostAsJsonAsync("/api/claims", await ClaimAsync(reportId));
        var body = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("score", body, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task A_reporter_claiming_their_own_report_gets_a_400()
    {
        var reportId = await CreateLostReportAsync();
        var request = (await ClaimAsync(reportId)) with { UniversityId = ReporterUniversityId };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("A reporter cannot claim their own report.", problem?.Detail);
    }

    [Fact]
    public async Task The_same_university_member_cannot_claim_twice()
    {
        var reportId = await CreateLostReportAsync();
        var request = await ClaimAsync(reportId);

        Assert.Equal(HttpStatusCode.Created, (await _client.PostAsJsonAsync("/api/claims", request)).StatusCode);

        var second = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
    }

    [Fact]
    public async Task A_report_that_is_still_waiting_for_handover_cannot_be_claimed()
    {
        var reportId = await CreateFoundReportAsync();
        var request = (await ClaimAsync(reportId)) with { HandoverPointId = null };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("Claims can only be opened on an open report.", problem?.Detail);
    }

    [Fact]
    public async Task An_unknown_report_is_a_404()
    {
        var request = (await ClaimAsync(await CreateLostReportAsync())) with { ReportId = Guid.NewGuid() };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task An_unknown_university_id_gets_a_400()
    {
        var request = (await ClaimAsync(await CreateLostReportAsync())) with { UniversityId = "UM-000000" };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_missing_secret_description_is_a_validation_problem()
    {
        var request = (await ClaimAsync(await CreateLostReportAsync())) with { SecretDescription = "" };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains(nameof(CreateClaimCommand.SecretDescription), problem!.Errors.Keys);
    }

    [Fact]
    public async Task A_claim_dated_in_the_future_is_rejected()
    {
        var tomorrow = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(1);
        var request = (await ClaimAsync(await CreateLostReportAsync())) with { LostOn = tomorrow };

        var response = await _client.PostAsJsonAsync("/api/claims", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.Equal("A claim cannot be dated in the future.", problem?.Detail);
    }

    [Fact]
    public async Task Answering_only_one_question_of_the_category_is_rejected()
    {
        var request = await ClaimAsync(await CreateLostReportAsync());

        var response = await _client.PostAsJsonAsync("/api/claims", request with { Answers = [request.Answers[0]] });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<Guid> CreateLostReportAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", await ReportAsync(ItemReportType.Lost));
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        return created!.Id;
    }

    private async Task<Guid> CreateFoundReportAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", await ReportAsync(ItemReportType.Found));
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        return created!.Id;
    }

    private async Task<CreateReportCommand> ReportAsync(ItemReportType type)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = await db.Categories.FirstAsync(c => c.Name == "Keys");
        var location = await db.UniversityLocations.FirstAsync(l => !l.IsHandoverPoint);
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.IsHandoverPoint);

        return new CreateReportCommand(
            type,
            ReporterUniversityId,
            category.Id,
            "Key ring with three keys",
            "Left it near the lockers.",
            location.Id,
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1),
            "A small brass bottle opener hangs on the ring.",
            await AnswersAsync(db, category.Id),
            type == ItemReportType.Found ? handoverPoint.Id : null);
    }

    private async Task<CreateClaimCommand> ClaimAsync(Guid reportId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var report = await db.ItemReports.SingleAsync(r => r.Id == reportId);
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.IsHandoverPoint);

        return new CreateClaimCommand(
            reportId,
            ClaimantUniversityId,
            "There is a brass bottle opener on the ring.",
            await AnswersAsync(db, report.CategoryId),
            LostOn: null,
            handoverPoint.Id);
    }

    private static async Task<List<SecretAnswer>> AnswersAsync(AppDbContext db, Guid categoryId)
    {
        var questions = await db.Questions.Where(q => q.CategoryId == categoryId).ToListAsync();
        var answers = new List<SecretAnswer>();

        foreach (var question in questions)
        {
            var option = await db.QuestionOptions.FirstAsync(o => o.QuestionId == question.Id);
            answers.Add(new SecretAnswer(question.Id, option.Id));
        }

        return answers;
    }
}
