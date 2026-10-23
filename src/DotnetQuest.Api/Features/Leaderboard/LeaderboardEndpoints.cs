using System.ComponentModel.DataAnnotations;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Features.Leaderboard;

public sealed record LeaderboardEntry(int Rank, string DisplayName, int TotalXp, int Level, string Title);

public static class LeaderboardEndpoints
{
    public const string AnonymousName = "Anonim Geliştirici";

    public static IEndpointRouteBuilder MapLeaderboardEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/leaderboard", GetLeaderboard)
            .WithTags("Leaderboard")
            .WithSummary("En çok XP toplayan oyuncuları listeler")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromSeconds(30)).SetVaryByQuery("top"));

        return app;
    }

    private static async Task<Ok<List<LeaderboardEntry>>> GetLeaderboard(
        AppDbContext db,
        CancellationToken cancellationToken,
        [Range(1, 100)] int top = 10)
    {
        var players = await db.Users
            .AsNoTracking()
            .Where(p => p.TotalXp > 0)
            .OrderByDescending(p => p.TotalXp)
            .ThenBy(p => p.Id)
            .Take(top)
            .Select(p => new { p.DisplayName, p.TotalXp })
            .ToListAsync(cancellationToken);

        var entries = players
            .Select((p, index) =>
            {
                var level = PlayerLevel.FromXp(p.TotalXp);
                return new LeaderboardEntry(index + 1, p.DisplayName ?? AnonymousName, p.TotalXp, level.Level, level.Title);
            })
            .ToList();

        return TypedResults.Ok(entries);
    }
}
