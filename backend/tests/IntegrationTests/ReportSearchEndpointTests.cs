using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Common;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class ReportSearchEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private const string SecretOnlyWord = "transit";


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
    public async Task The_list_returns_the_seeded_report()
    {
        var page = await SearchAsync("?q=wallet");

        var report = Assert.Single(page.Items);
        Assert.Equal("Black leather wallet", report.Title);
        Assert.Equal("Wallet & Cards", report.CategoryName);
        Assert.Equal("Cafeteria", report.LocationName);
        Assert.Equal(ItemReportStatus.Open, report.Status);
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task No_secret_field_reaches_the_wire_in_the_list()
    {
        var json = await _client.GetStringAsync("/api/reports");

        AssertNoSecrets(json);
    }

    [Fact]
    public async Task No_secret_field_reaches_the_wire_in_the_detail()
    {
        var id = (await SearchAsync("?q=wallet")).Items[0].Id;

        var json = await _client.GetStringAsync($"/api/reports/{id}");

        AssertNoSecrets(json);
        Assert.Contains("Left on a table in the cafeteria", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Searching_a_word_that_only_exists_in_a_secret_description_finds_nothing()
    {
        var page = await SearchAsync($"?q={SecretOnlyWord}");

        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task The_search_term_ignores_case()
    {
        var page = await SearchAsync("?q=WALLET");

        Assert.Single(page.Items);
    }

    [Fact]
    public async Task A_report_waiting_for_handover_is_invisible_in_the_list_and_in_the_detail()
    {
        var pending = await CreatePendingHandoverReportAsync();

        var page = await SearchAsync("?q=skateboard");
        var detail = await _client.GetAsync($"/api/reports/{pending}");

        Assert.Empty(page.Items);
        Assert.Equal(HttpStatusCode.NotFound, detail.StatusCode);
    }

    [Fact]
    public async Task An_unknown_id_is_a_404()
    {
        var response = await _client.GetAsync($"/api/reports/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Filters_narrow_the_result()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var wallet = await db.Categories.FirstAsync(c => c.Name == "Wallet & Cards");
        var cafeteria = await db.UniversityLocations.FirstAsync(l => l.Name == "Cafeteria");
        var reception = await db.UniversityLocations.FirstAsync(l => l.Name == "Main Reception");

        Assert.Single((await SearchAsync($"?type=Found&category={wallet.Id}")).Items);
        Assert.Empty((await SearchAsync($"?type=Lost&category={wallet.Id}")).Items);
        Assert.Single((await SearchAsync($"?category={wallet.Id}&location={cafeteria.Id}")).Items);
        Assert.Empty((await SearchAsync($"?category={wallet.Id}&location={reception.Id}")).Items);
        Assert.Single((await SearchAsync($"?category={wallet.Id}&from=2026-09-14&to=2026-09-14")).Items);
        Assert.Empty((await SearchAsync($"?category={wallet.Id}&to=2026-09-13")).Items);
    }

    [Fact]
    public async Task The_newest_report_comes_first_and_paging_never_repeats_a_row()
    {
        await CreateLostAsync("Ordering probe one", new DateOnly(2026, 9, 10));
        await CreateLostAsync("Ordering probe two", new DateOnly(2026, 9, 12));
        await CreateLostAsync("Ordering probe three", new DateOnly(2026, 9, 11));

        var all = await SearchAsync("?q=ordering probe");
        var firstPage = await SearchAsync("?q=ordering probe&pageSize=2");
        var secondPage = await SearchAsync("?q=ordering probe&page=2&pageSize=2");

        Assert.Equal(
            ["Ordering probe two", "Ordering probe three", "Ordering probe one"],
            all.Items.Select(i => i.Title));
        Assert.Equal(3, all.TotalCount);
        Assert.Equal(all.Items.Take(2).Select(i => i.Id), firstPage.Items.Select(i => i.Id));
        Assert.Equal(all.Items.Skip(2).Select(i => i.Id), secondPage.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Paging_reports_the_total_independently_of_the_page_size()
    {
        var page = await SearchAsync("?q=wallet&page=2&pageSize=1");

        Assert.Empty(page.Items);
        Assert.Equal(1, page.TotalCount);
        Assert.Equal(2, page.Page);
    }

    [Fact]
    public async Task A_returned_report_leaves_the_default_list_but_keeps_its_own_filter()
    {
        var before = await SetSeededReportStatusAsync(ItemReportStatus.Returned);
        try
        {
            Assert.Empty((await SearchAsync("?q=wallet")).Items);
            Assert.Single((await SearchAsync("?q=wallet&status=Returned")).Items);
        }
        finally
        {
            // The fixture is shared by every test in this class, so put the row back.
            await SetSeededReportStatusAsync(before);
        }
    }

    [Theory]
    [InlineData("?status=Closed")]
    [InlineData("?status=PendingHandover")]
    [InlineData("?pageSize=101")]
    [InlineData("?page=0")]
    [InlineData("?from=2026-09-10&to=2026-09-01")]
    [InlineData("?status=NotAStatus")]
    public async Task An_unusable_query_is_a_400_and_never_a_silent_empty_page(string queryString)
    {
        var response = await _client.GetAsync($"/api/reports{queryString}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task A_created_report_points_at_its_public_detail()
    {
        var response = await _client.PostAsJsonAsync("/api/reports", await LostRequestAsync());
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal($"/api/reports/{created!.Id}", response.Headers.Location?.AbsolutePath);

        var detail = await _client.GetFromJsonAsync<ItemReportDetailDto>(response.Headers.Location, StringEnumJson);
        Assert.Equal(created.Id, detail!.Id);
    }

    private static void AssertNoSecrets(string json)
    {
        Assert.DoesNotContain("secret", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(SecretOnlyWord, json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("handover", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("reporter", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("trackingCode", json, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<PagedResult<ItemReportSummaryDto>> SearchAsync(string queryString)
        => (await _client.GetFromJsonAsync<PagedResult<ItemReportSummaryDto>>(
            $"/api/reports{queryString}", StringEnumJson))!;

    /// <summary>Reaching Returned through the domain needs a claim, an approval and a handover;
    /// the search only cares about the column, so the status is set straight on the row.</summary>
    private async Task<ItemReportStatus> SetSeededReportStatusAsync(ItemReportStatus status)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var report = await db.ItemReports.FirstAsync(r => r.Title == "Black leather wallet");

        var before = report.Status;
        db.Entry(report).Property(r => r.Status).CurrentValue = status;
        await db.SaveChangesAsync();
        return before;
    }

    private async Task CreateLostAsync(string title, DateOnly occurredOn)
    {
        var request = (await LostRequestAsync()) with { Title = title, OccurredOn = occurredOn };
        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private async Task<Guid> CreatePendingHandoverReportAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.IsHandoverPoint);

        var request = (await LostRequestAsync()) with
        {
            Type = ItemReportType.Found,
            Title = "Blue skateboard",
            HandoverPointId = handoverPoint.Id,
        };

        var response = await _client.PostAsJsonAsync("/api/reports", request);
        var created = await response.Content.ReadFromJsonAsync<CreatedReportDto>();
        return created!.Id;
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
}
