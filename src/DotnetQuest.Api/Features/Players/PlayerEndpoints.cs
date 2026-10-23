using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using DotnetQuest.Api.Common;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Features.Players;

public sealed record PlayerProfileResponse(
    string Email,
    string? DisplayName,
    int TotalXp,
    int Level,
    string Title,
    int LevelStartXp,
    int? NextLevelXp,
    int Rank,
    int SessionsCompleted,
    int AnswersGiven,
    int CorrectAnswers);

public sealed record UpdateProfileRequest(
    [Required]
    [StringLength(24, MinimumLength = 3)]
    [RegularExpression(@"^[\p{L}\p{N} _.\-]+$", ErrorMessage = "Sadece harf, rakam, boşluk ve _ . - kullanılabilir.")]
    string DisplayName);

public static class PlayerEndpoints
{
    public static IEndpointRouteBuilder MapPlayerEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/players").WithTags("Players").RequireAuthorization();

        group.MapGet("/me", GetMe).WithSummary("Giriş yapmış oyuncunun profilini ve istatistiklerini getirir");
        group.MapPut("/me", UpdateMe).WithSummary("Görünen adı günceller");

        return app;
    }

    private static async Task<Results<Ok<PlayerProfileResponse>, NotFound>> GetMe(
        ClaimsPrincipal user,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var playerId = user.GetPlayerId();

        var player = await db.Users
            .AsNoTracking()
            .Where(p => p.Id == playerId)
            .Select(p => new { p.Email, p.DisplayName, p.TotalXp })
            .SingleOrDefaultAsync(cancellationToken);

        if (player is null)
        {
            return TypedResults.NotFound();
        }

        // DbContext thread-safe olmadığı için sorgular sırayla (paralel değil) çalıştırılır.
        var rank = await db.Users.CountAsync(p => p.TotalXp > player.TotalXp, cancellationToken) + 1;
        var sessionsCompleted = await db.GameSessions.CountAsync(s => s.PlayerId == playerId && s.CompletedAtUtc != null, cancellationToken);
        var answers = db.GameSessions.Where(s => s.PlayerId == playerId).SelectMany(s => s.Questions).Where(q => q.AnsweredAtUtc != null);
        var answersGiven = await answers.CountAsync(cancellationToken);
        var correctAnswers = await answers.CountAsync(q => q.IsCorrect == true, cancellationToken);

        var level = PlayerLevel.FromXp(player.TotalXp);

        return TypedResults.Ok(new PlayerProfileResponse(
            player.Email!,
            player.DisplayName,
            player.TotalXp,
            level.Level,
            level.Title,
            level.LevelStartXp,
            level.NextLevelXp,
            rank,
            sessionsCompleted,
            answersGiven,
            correctAnswers));
    }

    private static async Task<Results<NoContent, NotFound>> UpdateMe(
        UpdateProfileRequest request,
        ClaimsPrincipal user,
        AppDbContext db,
        CancellationToken cancellationToken)
    {
        var playerId = user.GetPlayerId();
        var updated = await db.Users
            .Where(p => p.Id == playerId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.DisplayName, request.DisplayName.Trim()), cancellationToken);

        return updated == 0 ? TypedResults.NotFound() : TypedResults.NoContent();
    }

    /// <summary>Identity API'de logout endpoint'i yok; cookie'yi temizlemek için ekliyoruz.</summary>
    public static IEndpointRouteBuilder MapLogout(this IEndpointRouteBuilder auth)
    {
        auth.MapPost("/logout", async (SignInManager<Player> signInManager) =>
            {
                await signInManager.SignOutAsync();
                return TypedResults.NoContent();
            })
            .RequireAuthorization()
            .WithSummary("Oturumu kapatır (cookie'yi siler)");

        return auth;
    }
}
