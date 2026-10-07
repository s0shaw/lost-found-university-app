using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UniversityLostFound.Application.Catalog;
using UniversityLostFound.Application.Reports;
using UniversityLostFound.Domain.Reports;
using UniversityLostFound.Infrastructure.Persistence;
using UniversityLostFound.Infrastructure.Persistence.Seed;

namespace UniversityLostFound.IntegrationTests;

public sealed class PublicLookupEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>, IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 12, 0, 0, TimeSpan.Zero);

    private const string Reporter = "UM-358027";
    private const string Bystander = "UM-641293";

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
    public async Task The_catalog_carries_every_category_with_its_questions_and_options()
    {
        var categories = await _client.GetFromJsonAsync<IReadOnlyList<CategoryDto>>("/api/categories");

        Assert.Equal("Electronics", categories![0].Name);
        Assert.Equal("Other", categories[^1].Name);
        Assert.All(categories, c => Assert.All(c.Questions, q => Assert.NotEmpty(q.Options)));

        var wallet = categories.Single(c => c.Name == "Wallet & Cards");
        Assert.Contains(wallet.Questions, q => q.Text == "Were there cards inside?");
        Assert.All(wallet.Questions, q => Assert.Contains(q.Options, o => o.Text == "Don't know"));
    }

    [Fact]
    public async Task The_locations_say_which_ones_accept_a_handover()
    {
        var locations = (await _client.GetFromJsonAsync<IReadOnlyList<LocationDto>>("/api/locations"))!;

        Assert.True(locations.Single(l => l.Name == "Main Reception").IsHandoverPoint);
        Assert.False(locations.Single(l => l.Name == "Cafeteria").IsHandoverPoint);
    }

    [Fact]
    public async Task A_reporter_sees_their_own_secret_data_with_the_answers_spelled_out()
    {
        var created = await CreateLostAsync(Reporter);

        var tracked = await TrackAsync(Reporter, created.TrackingCode);

        Assert.Equal(created.Id, tracked.Id);
        Assert.Equal(created.TrackingCode, tracked.TrackingCode);
        Assert.Equal(ItemReportStatus.Open, tracked.Status);
        Assert.Equal("It has a wooden handle with initials.", tracked.SecretDescription);
        Assert.Null(tracked.HandoverPointName);

        // The request answered every question with its first option, in display order.
        Assert.Equal(
            [
                ("What colour was it?", "Black"),
                ("How big was it?", "Small (pocket-sized)"),
                ("Any distinguishing mark?", "Sticker"),
                ("What kind of item?", "Glasses"),
            ],
            tracked.Answers.Select(a => (a.Question, a.Answer)));
    }

    [Fact]
    public async Task A_report_waiting_for_handover_is_still_visible_to_the_one_who_reported_it()
    {
        var created = await CreateFoundAsync(Reporter);

        var tracked = await TrackAsync(Reporter, created.TrackingCode);

        Assert.Equal(ItemReportStatus.PendingHandover, tracked.Status);
        Assert.Equal("Main Reception", tracked.HandoverPointName);
    }

    [Fact]
    public async Task A_code_typed_in_lower_case_still_finds_the_report()
    {
        var created = await CreateLostAsync(Reporter);

        var tracked = await TrackAsync(Reporter.ToLowerInvariant(), created.TrackingCode.ToLowerInvariant());

        Assert.Equal(created.Id, tracked.Id);
    }

    [Fact]
    public async Task Someone_else_holding_the_tracking_code_gets_nothing()
    {
        var created = await CreateLostAsync(Reporter);

        var response = await PostTrackAsync(Bystander, created.TrackingCode);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task A_wrong_university_id_a_wrong_code_and_an_unknown_pair_answer_the_same_way()
    {
        var created = await CreateLostAsync(Reporter);

        var wrongMember = await ReadProblemAsync(Bystander, created.TrackingCode);
        var wrongCode = await ReadProblemAsync(Reporter, "LF-000000");
        var unknownMember = await ReadProblemAsync("UM-000000", created.TrackingCode);
        var nothingKnown = await ReadProblemAsync("UM-000000", "LF-000000");

        Assert.Equal(TrackReportHandler.NotFoundMessage, wrongMember);
        Assert.Equal(wrongMember, wrongCode);
        Assert.Equal(wrongMember, unknownMember);
        Assert.Equal(wrongMember, nothingKnown);
    }

    [Fact]
    public async Task An_empty_tracking_request_is_a_400()
    {
        var response = await PostTrackAsync(string.Empty, string.Empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<TrackedReportDto> TrackAsync(string universityId, string trackingCode)
    {
        var response = await PostTrackAsync(universityId, trackingCode);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TrackedReportDto>(StringEnumJson))!;
    }

    private Task<HttpResponseMessage> PostTrackAsync(string universityId, string trackingCode)
        => _client.PostAsJsonAsync("/api/track", new TrackReportCommand(universityId, trackingCode));

    private async Task<string?> ReadProblemAsync(string universityId, string trackingCode)
    {
        var response = await PostTrackAsync(universityId, trackingCode);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ProblemDetails>())!.Detail;
    }

    private async Task<CreatedReportDto> CreateLostAsync(string universityId)
        => await CreateAsync(await LostRequestAsync(universityId));

    private async Task<CreatedReportDto> CreateFoundAsync(string universityId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var handoverPoint = await db.UniversityLocations.FirstAsync(l => l.Name == "Main Reception");

        var request = (await LostRequestAsync(universityId)) with
        {
            Type = ItemReportType.Found,
            Title = "Grey scarf",
            HandoverPointId = handoverPoint.Id,
        };

        return await CreateAsync(request);
    }

    private async Task<CreatedReportDto> CreateAsync(CreateReportCommand request)
    {
        var response = await _client.PostAsJsonAsync("/api/reports", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<CreatedReportDto>())!;
    }

    private async Task<CreateReportCommand> LostRequestAsync(string universityId)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var category = await db.Categories.FirstAsync(c => c.Name == "Clothing & Accessories");
        var questions = await db.Questions.Where(q => q.CategoryId == category.Id).ToListAsync();
        var answers = new List<SecretAnswer>();

        foreach (var question in questions)
        {
            var option = await db.QuestionOptions
                .Where(o => o.QuestionId == question.Id)
                .OrderBy(o => o.DisplayOrder)
                .FirstAsync();
            answers.Add(new SecretAnswer(question.Id, option.Id));
        }

        var location = await db.UniversityLocations.FirstAsync(l => l.Name == "Cafeteria");

        return new CreateReportCommand(
            ItemReportType.Lost,
            universityId,
            category.Id,
            "Grey scarf",
            "Left it on a chair.",
            location.Id,
            DateOnly.FromDateTime(Now.UtcDateTime).AddDays(-1),
            "It has a wooden handle with initials.",
            answers,
            HandoverPointId: null);
    }
}
