using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class CancelReportTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private const string ReporterUniversityId = "UM-482915";
    private const string OtherUniversityId = "UM-730164";

    private readonly ApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();
    private Guid _trackingReportId;
    private string _trackingCode = string.Empty;

    public Task InitializeAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task The_owner_cancels_and_the_report_leaves_the_public_list()
    {
        var reportId = await CreateLostReportAsync();

        var response = await _client.PostAsJsonAsync(
            "/api/track/cancel", new { universityId = ReporterUniversityId, trackingCode = _trackingCode });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/reports/{reportId}")).StatusCode);
    }

    [Fact]
    public async Task Someone_else_gets_the_same_404_as_the_tracking_endpoint()
    {
        await CreateLostReportAsync();

        var cancel = await _client.PostAsJsonAsync(
            "/api/track/cancel", new { universityId = OtherUniversityId, trackingCode = _trackingCode });
        var track = await _client.PostAsJsonAsync(
            "/api/track", new { universityId = OtherUniversityId, trackingCode = _trackingCode });

        Assert.Equal(HttpStatusCode.NotFound, cancel.StatusCode);

        // Detail, not the raw body: the body also carries a traceId that differs per request.
        var cancelDetail = (await cancel.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;
        var trackDetail = (await track.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;
        Assert.Equal(trackDetail, cancelDetail);
    }

    [Fact]
    public async Task Staff_cancel_through_their_own_route()
    {
        var reportId = await CreateLostReportAsync();
        var staff = await _factory.LoginAsStaffAsync();

        var response = await staff.PostAsync($"/api/staff/reports/{reportId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task An_anonymous_caller_cannot_use_the_staff_cancel_route()
    {
        var reportId = await CreateLostReportAsync();

        var response = await _client.PostAsync($"/api/staff/reports/{reportId}/cancel", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private async Task<Guid> CreateLostReportAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await DatabaseSeeder.SeedAsync(db, Now);

        var category = await db.Categories.FirstAsync(c => c.Name == "Keys");
        var location = await db.UniversityLocations.FirstAsync(l => !l.IsHandoverPoint);

        var command = new CreateReportCommand(
            ItemReportType.Lost,
            ReporterUniversityId,
            category.Id,
            "Key ring with three keys",
            "Left it near the lockers.",
            location.Id,
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1),
            "A small brass bottle opener hangs on the ring.",
            await AnswersAsync(db, category.Id),
            HandoverPointId: null);

        var response = await _client.PostAsJsonAsync("/api/reports", command);
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        _trackingReportId = created!.Id;
        _trackingCode = created.TrackingCode;

        return _trackingReportId;
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
