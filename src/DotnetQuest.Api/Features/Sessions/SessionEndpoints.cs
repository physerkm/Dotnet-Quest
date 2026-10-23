using System.Security.Claims;
using DotnetQuest.Api.Common;
using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Features.Sessions;

public static class SessionEndpoints
{
    private const string GetSessionRoute = "GetSession";

    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/sessions")
            .WithTags("Game Sessions")
            .RequireRateLimiting(RateLimitPolicies.Gameplay);

        group.MapPost("/", CreateSession)
            .WithSummary("Yeni bir oyun turu başlatır")
            .WithDescription("Seçilen kategori ve zorlukta rastgele sorularla bir tur oluşturur. Giriş yapılmışsa tur oyuncuya bağlanır ve XP kazandırır.");

        group.MapGet("/{id:guid}", GetSession)
            .WithName(GetSessionRoute)
            .WithSummary("Bir oyun turunu getirir");

        group.MapPost("/{id:guid}/answers", SubmitAnswer)
            .WithSummary("Bir soruya cevap gönderir")
            .WithDescription("Cevabı sunucuda kontrol eder, puanı günceller ve açıklamayı döner. Son soru cevaplanınca tur tamamlanır.");

        return app;
    }

    private static async Task<Results<CreatedAtRoute<SessionResponse>, ValidationProblem>> CreateSession(
        CreateSessionRequest request,
        AppDbContext db,
        TimeProvider clock,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var categoryExists = await db.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (!categoryExists)
        {
            return ApiProblems.Validation("categoryId", "Böyle bir kategori yok.");
        }

        var questionIds = await db.Questions
            .Where(q => q.CategoryId == request.CategoryId && q.Difficulty == request.Difficulty)
            .OrderBy(_ => EF.Functions.Random())
            .Select(q => q.Id)
            .Take(GameSession.QuestionCount)
            .ToListAsync(cancellationToken);

        if (questionIds.Count == 0)
        {
            return ApiProblems.Validation("difficulty", "Bu kategoride bu zorlukta henüz soru yok.");
        }

        var session = GameSession.Start(
            request.CategoryId,
            request.Difficulty,
            user.GetPlayerId(),
            questionIds,
            clock.GetUtcNow().UtcDateTime);

        db.GameSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);

        var created = await FindSessionAsync(db, session.Id, user, asNoTracking: true, cancellationToken);

        // 201 Created + Location: /api/sessions/{id}
        return TypedResults.CreatedAtRoute(SessionMapper.ToResponse(created!), GetSessionRoute, new { id = session.Id });
    }

    private static async Task<Results<Ok<SessionResponse>, NotFound>> GetSession(
        Guid id,
        AppDbContext db,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(db, id, user, asNoTracking: true, cancellationToken);

        return session is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(SessionMapper.ToResponse(session));
    }

    private static async Task<Results<Ok<AnswerResponse>, NotFound, ValidationProblem, ProblemHttpResult>> SubmitAnswer(
        Guid id,
        SubmitAnswerRequest request,
        AppDbContext db,
        TimeProvider clock,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var session = await FindSessionAsync(db, id, user, asNoTracking: false, cancellationToken);
        if (session is null)
        {
            return TypedResults.NotFound();
        }

        if (session.IsCompleted)
        {
            return ApiProblems.Conflict("Bu oyun turu zaten tamamlandı.");
        }

        var sessionQuestion = session.Questions.SingleOrDefault(q => q.QuestionId == request.QuestionId);
        if (sessionQuestion is null)
        {
            return ApiProblems.Validation("questionId", "Bu soru bu oyun turuna ait değil.");
        }

        if (sessionQuestion.IsAnswered)
        {
            return ApiProblems.Conflict("Bu soru zaten cevaplandı.");
        }

        var check = AnswerEvaluator.Evaluate(sessionQuestion.Question, session.Id, request);
        if (!check.IsValid)
        {
            return ApiProblems.Validation(check.ErrorField!, check.ErrorMessage!);
        }

        session.RecordAnswer(sessionQuestion.QuestionId, check.IsCorrect, clock.GetUtcNow().UtcDateTime);
        var xpReward = session.CalculateXpReward();

        // Cevap kaydı ve XP artışı ya birlikte olur ya hiç olmaz.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            // Aynı tur için iki cevap aynı anda geldi; Version token'ı ikincisini reddetti.
            return ApiProblems.Conflict("Bu tura aynı anda başka bir cevap gönderildi. Sayfayı yenileyip tekrar dene.");
        }

        var awardXp = session.IsCompleted && session.PlayerId is not null;
        if (awardXp)
        {
            // Atomik artış: "oku → topla → yaz" yerine tek bir UPDATE (yarış durumuna karşı güvenli).
            await db.Users
                .Where(p => p.Id == session.PlayerId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.TotalXp, p => p.TotalXp + xpReward), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        return TypedResults.Ok(new AnswerResponse(
            SessionMapper.ToFeedback(session.Id, sessionQuestion),
            session.Score,
            session.MaxScore,
            session.IsCompleted,
            session.IsPerfect,
            awardXp ? xpReward : null));
    }

    /// <summary>
    /// Turu getirir. Başka bir oyuncuya ait turlar için null döner (403 yerine 404: turun varlığını sızdırmayız).
    /// </summary>
    private static async Task<GameSession?> FindSessionAsync(
        AppDbContext db,
        Guid id,
        ClaimsPrincipal user,
        bool asNoTracking,
        CancellationToken cancellationToken)
    {
        IQueryable<GameSession> query = db.GameSessions
            .Include(s => s.Category)
            .Include(s => s.Questions)
            .ThenInclude(q => q.Question);

        if (asNoTracking)
        {
            query = query.AsNoTracking();
        }

        var session = await query.SingleOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (session?.PlayerId is not null && session.PlayerId != user.GetPlayerId())
        {
            return null;
        }

        return session;
    }
}
