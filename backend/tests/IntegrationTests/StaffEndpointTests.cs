using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Claims;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Application.Staff;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class StaffEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 18, 12, 0, 0, TimeSpan.Zero);

    private static readonly JsonSerializerOptions StringEnumJson =
        new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };

    private const string ReporterUniversityId = "UM-482915";
    private const string ClaimantUniversityId = "UM-730164";
    private const string OtherClaimantUniversityId = "UM-358027";
    private const string SecretText = "Scratch on the top left corner, a blue bus card inside.";

    private readonly ApiFactory _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    public async Task InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        await DatabaseSeeder.SeedAsync(scope.ServiceProvider.GetRequiredService<AppDbContext>(), Now);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Theory]
    [InlineData("GET", "/api/staff/reports")]
    [InlineData("GET", "/api/staff/claims")]
    [InlineData("POST", "/api/staff/reports/{id}/confirm-handover")]
    [InlineData("POST", "/api/staff/reports/{id}/mark-returned")]
    [InlineData("POST", "/api/staff/reports/{id}/close")]
    [InlineData("POST", "/api/staff/reports/{id}/cancel")]
    [InlineData("POST", "/api/staff/claims/{id}/approve")]
    [InlineData("POST", "/api/staff/claims/{id}/reject")]
    public async Task Every_staff_route_refuses_an_anonymous_caller(string method, string route)
    {
        var response = await _client.SendAsync(new HttpRequestMessage(
            new HttpMethod(method), route.Replace("{id}", Guid.NewGuid().ToString(), StringComparison.Ordinal)));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Confirming_a_handover_puts_a_found_report_on_the_public_list()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var reportId = await CreateFoundReportAsync();

        // The possession gate is still shut.
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/reports/{reportId}")).StatusCode);

        var confirmed = await staff.PostAsync($"/api/staff/reports/{reportId}/confirm-handover", content: null);

        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync($"/api/reports/{reportId}")).StatusCode);
    }

    [Fact]
    public async Task The_pending_handover_queue_lists_what_the_public_cannot_see()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var reportId = await CreateFoundReportAsync();

        var queue = await staff.GetFromJsonAsync<PagedResult<StaffReportSummaryDto>>(
            "/api/staff/reports?queue=PendingHandover", StringEnumJson);

        Assert.Contains(queue!.Items, r => r.Id == reportId);
    }

    [Fact]
    public async Task The_duplicate_queue_pages_past_its_first_page()
    {
        var staff = await _factory.LoginAsStaffAsync();

        // Three identical lost reports: every pair scores above the duplicate threshold.
        await CreateLostReportAsync();
        await CreateLostReportAsync();
        await CreateLostReportAsync();

        var first = await staff.GetFromJsonAsync<PagedResult<StaffReportSummaryDto>>(
            "/api/staff/reports?queue=PossibleDuplicate&pageSize=2&page=1", StringEnumJson);
        var second = await staff.GetFromJsonAsync<PagedResult<StaffReportSummaryDto>>(
            "/api/staff/reports?queue=PossibleDuplicate&pageSize=2&page=2", StringEnumJson);

        Assert.True(first!.TotalCount >= 3);
        Assert.Equal(2, first.Items.Count);

        // Skipping a second time in sql emptied every page but the first.
        Assert.NotEmpty(second!.Items);
        Assert.Empty(first.Items.Select(r => r.Id).Intersect(second.Items.Select(r => r.Id)));
        Assert.All(second.Items, r => Assert.NotEmpty(r.PossibleDuplicateIds));
    }

    [Fact]
    public async Task Approving_a_claim_matches_the_report_and_rejects_the_rival()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var (reportId, winner, loser) = await CreateReportWithTwoClaimsAsync();

        var response = await staff.PostAsync($"/api/staff/claims/{winner}/approve", content: null);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var detail = await staff.GetFromJsonAsync<StaffReportDetailDto>(
            $"/api/staff/reports/{reportId}", StringEnumJson);
        Assert.Equal(ItemReportStatus.Matched, detail!.Report.Status);
        Assert.Equal(ClaimStatus.Approved, detail.Claims.Single(c => c.Id == winner).Status);
        Assert.Equal(ClaimStatus.Rejected, detail.Claims.Single(c => c.Id == loser).Status);
    }

    [Fact]
    public async Task A_rejection_without_a_note_is_a_400()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var (_, claimId, _) = await CreateReportWithTwoClaimsAsync();

        var response = await staff.PostAsJsonAsync($"/api/staff/claims/{claimId}/reject", new { staffNote = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task The_staff_view_carries_the_secret_the_public_view_hides()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var reportId = await CreateLostReportAsync();

        var body = await (await staff.GetAsync($"/api/staff/reports/{reportId}")).Content.ReadAsStringAsync();

        Assert.Contains("secretDescription", body, StringComparison.Ordinal);
        Assert.Contains(SecretText, body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task The_comparison_screen_returns_a_breakdown_that_is_not_stored_anywhere()
    {
        var staff = await _factory.LoginAsStaffAsync();
        var (_, claimId, _) = await CreateReportWithTwoClaimsAsync();

        var comparison = await staff.GetFromJsonAsync<StaffClaimComparisonDto>(
            $"/api/staff/claims/{claimId}", StringEnumJson);

        Assert.Equal(comparison!.Claim.Score, comparison.Score);
        Assert.Equal(comparison.TotalAnswers, comparison.Answers.Count);
        Assert.All(comparison.Answers, a => Assert.NotNull(a.Question));
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

    private async Task<(Guid ReportId, Guid Winner, Guid Loser)> CreateReportWithTwoClaimsAsync()
    {
        var reportId = await CreateLostReportAsync();

        var winner = await _client.PostAsJsonAsync(
            "/api/claims", await ClaimAsync(reportId, ClaimantUniversityId, SecretText));
        var loser = await _client.PostAsJsonAsync(
            "/api/claims", await ClaimAsync(reportId, OtherClaimantUniversityId, "A guess with nothing in common."));

        var winnerId = (await winner.Content.ReadFromJsonAsync<CreatedClaimDto>(StringEnumJson))!.Id;
        var loserId = (await loser.Content.ReadFromJsonAsync<CreatedClaimDto>(StringEnumJson))!.Id;

        return (reportId, winnerId, loserId);
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
            SecretText,
            await AnswersAsync(db, category.Id),
            type == ItemReportType.Found ? handoverPoint.Id : null);
    }

    private async Task<CreateClaimCommand> ClaimAsync(Guid reportId, string claimantUniversityId, string secretDescription)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var report = await db.ItemReports.SingleAsync(r => r.Id == reportId);
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.IsHandoverPoint);

        return new CreateClaimCommand(
            reportId,
            claimantUniversityId,
            secretDescription,
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
