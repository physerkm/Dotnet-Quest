using System.Net;
using System.Net.Http.Json;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using DotnetQuest.Api.Features.Categories;
using DotnetQuest.Api.Features.Players;
using DotnetQuest.Api.Features.Sessions;
using DotnetQuest.Api.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetQuest.Api.Tests;

[Collection(ApiCollection.Name)]
public sealed class SessionEndpointsTests(ApiFactory factory)
{
    private static readonly System.Text.Json.JsonSerializerOptions Json = ApiFactory.Json;

    [Fact]
    public async Task GetCategories_ReturnsAllCategoriesWithQuestionsForEveryDifficulty()
    {
        var client = factory.CreateClient();

        var categories = await client.GetFromJsonAsync<List<CategoryResponse>>("/api/categories", Json);

        Assert.NotNull(categories);
        Assert.Equal(7, categories.Count);
        Assert.All(categories, c =>
        {
            Assert.True(c.QuestionCounts.Easy >= GameSession.QuestionCount, $"{c.Id} kolay soru eksik");
            Assert.True(c.QuestionCounts.Medium >= GameSession.QuestionCount, $"{c.Id} orta soru eksik");
            Assert.True(c.QuestionCounts.Hard >= GameSession.QuestionCount, $"{c.Id} zor soru eksik");
        });
    }

    [Fact]
    public async Task CreateSession_Returns201WithLocation_AndDoesNotLeakAnswers()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("http", Difficulty.Medium), Json);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<SessionResponse>(Json);
        Assert.NotNull(session);
        Assert.Equal($"/api/sessions/{session.Id}", response.Headers.Location?.AbsolutePath);
        Assert.Equal(GameSession.QuestionCount, session.Questions.Count);
        Assert.All(session.Questions, q => Assert.Null(q.Feedback));

        var raw = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("explanation", raw, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task CreateSession_UnknownCategory_Returns400ValidationProblem()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest("yok-boyle-bir-sey", Difficulty.Easy), Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("categoryId", problem!.Errors.Keys);
    }

    [Fact]
    public async Task SubmitAnswer_WithoutAnswerField_Returns400()
    {
        var client = factory.CreateClient();
        var session = await StartSessionAsync(client, "bughunt", Difficulty.Easy);

        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/answers",
            new SubmitAnswerRequest(session.Questions[0].Id, SelectedOption: null, SelectedLine: null, Ordering: null),
            Json);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PerfectGame_AwardsBonusXp_AndRejectsFurtherAnswers()
    {
        var client = await factory.CreateLoggedInClientAsync("Mükemmel Oyuncu");
        var session = await StartSessionAsync(client, "bughunt", Difficulty.Easy);
        var correctLines = await GetCorrectLinesAsync(session);

        AnswerResponse? last = null;
        foreach (var question in session.Questions)
        {
            var response = await client.PostAsJsonAsync(
                $"/api/sessions/{session.Id}/answers",
                new SubmitAnswerRequest(question.Id, null, correctLines[question.Id], null),
                Json);
            response.EnsureSuccessStatusCode();
            last = await response.Content.ReadFromJsonAsync<AnswerResponse>(Json);
            Assert.True(last!.Feedback.IsCorrect);
        }

        // 5 soru x 10 puan = 50, hatasız tur bonusu %25 = 12
        Assert.True(last!.SessionCompleted);
        Assert.True(last.IsPerfect);
        Assert.Equal(50, last.SessionScore);
        Assert.Equal(62, last.XpEarned);

        var profile = await client.GetFromJsonAsync<PlayerProfileResponse>("/api/players/me", Json);
        Assert.Equal(62, profile!.TotalXp);
        Assert.Equal("Junior Developer", profile.Title);
        Assert.Equal(5, profile.CorrectAnswers);

        var again = await client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/answers",
            new SubmitAnswerRequest(session.Questions[0].Id, null, 1, null),
            Json);
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task MultipleChoice_ShuffledOptions_AreMappedBackCorrectly()
    {
        var client = factory.CreateClient();
        var session = await StartSessionAsync(client, "http", Difficulty.Easy);
        var question = session.Questions[0];

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entity = await db.Questions.AsNoTracking().SingleAsync(q => q.Id == question.Id);
        var correctText = entity.Options[entity.CorrectOption!.Value];
        var displayedIndex = question.Options.ToList().IndexOf(correctText);

        var response = await client.PostAsJsonAsync(
            $"/api/sessions/{session.Id}/answers",
            new SubmitAnswerRequest(question.Id, displayedIndex, null, null),
            Json);

        var result = await response.Content.ReadFromJsonAsync<AnswerResponse>(Json);
        Assert.True(result!.Feedback.IsCorrect);
        Assert.Equal(displayedIndex, result.Feedback.CorrectOption);
    }

    [Fact]
    public async Task Session_OfAnotherPlayer_Returns404()
    {
        var owner = await factory.CreateLoggedInClientAsync();
        var session = await StartSessionAsync(owner, "di", Difficulty.Hard);

        var stranger = factory.CreateClient();
        var response = await stranger.GetAsync($"/api/sessions/{session.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PlayerProfile_WithoutLogin_Returns401()
    {
        var response = await factory.CreateClient().GetAsync("/api/players/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404ProblemDetails()
    {
        var response = await factory.CreateClient().GetAsync("/api/boyle-bir-endpoint-yok");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    private static async Task<SessionResponse> StartSessionAsync(HttpClient client, string categoryId, Difficulty difficulty)
    {
        var response = await client.PostAsJsonAsync("/api/sessions", new CreateSessionRequest(categoryId, difficulty), Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<SessionResponse>(Json))!;
    }

    private async Task<Dictionary<int, int>> GetCorrectLinesAsync(SessionResponse session)
    {
        var ids = session.Questions.Select(q => q.Id).ToList();

        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await db.Questions
            .Where(q => ids.Contains(q.Id))
            .ToDictionaryAsync(q => q.Id, q => q.CorrectLine!.Value);
    }
}
