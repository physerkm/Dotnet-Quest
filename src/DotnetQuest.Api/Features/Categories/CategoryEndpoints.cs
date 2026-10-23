using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Features.Categories;

public sealed record CategoryResponse(string Id, string Title, string Description, string Icon, QuestionCounts QuestionCounts);

public sealed record QuestionCounts(int Easy, int Medium, int Hard);

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/categories", GetCategories)
            .WithTags("Categories")
            .WithSummary("Kategorileri ve her zorluktaki soru sayısını listeler")
            .CacheOutput(policy => policy.Expire(TimeSpan.FromMinutes(5)));

        return app;
    }

    private static async Task<Ok<List<CategoryResponse>>> GetCategories(AppDbContext db, CancellationToken cancellationToken)
    {
        // Select ile projeksiyon: sadece gereken kolonlar çekilir, sayımlar SQL'de yapılır.
        var categories = await db.Categories
            .AsNoTracking()
            .OrderBy(c => c.SortOrder)
            .Select(c => new CategoryResponse(
                c.Id,
                c.Title,
                c.Description,
                c.Icon,
                new QuestionCounts(
                    c.Questions.Count(q => q.Difficulty == Difficulty.Easy),
                    c.Questions.Count(q => q.Difficulty == Difficulty.Medium),
                    c.Questions.Count(q => q.Difficulty == Difficulty.Hard))))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok(categories);
    }
}
