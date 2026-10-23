using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetQuest.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace DotnetQuest.Api.Data;

/// <summary>
/// Data/Seed/*.json dosyalarındaki kategori ve soruları veritabanına yazar.
/// "Upsert" mantığıyla çalışır: JSON'da bir soruyu düzeltip uygulamayı yeniden başlatmak yeterlidir.
/// </summary>
public static class ContentSeeder
{
    private const string ResourcePrefix = "DotnetQuest.Api.Data.Seed.";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static async Task SeedAsync(DbContext context, CancellationToken cancellationToken)
    {
        var db = (AppDbContext)context;

        var categories = await db.Categories.ToDictionaryAsync(c => c.Id, cancellationToken);
        var questions = await db.Questions.ToDictionaryAsync(q => q.Key, cancellationToken);

        foreach (var file in LoadSeedFiles())
        {
            var category = UpsertCategory(db, categories, file.Category);

            foreach (var seed in file.Questions)
            {
                UpsertQuestion(db, questions, category.Id, seed);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Gömülü JSON dosyalarını okur ve içeriği doğrular. Hatalı içerikte uygulama başlamaz (fail fast).</summary>
    public static IReadOnlyList<SeedFile> LoadSeedFiles()
    {
        var assembly = typeof(ContentSeeder).Assembly;
        var files = new List<SeedFile>();

        foreach (var name in assembly.GetManifestResourceNames().Where(n => n.StartsWith(ResourcePrefix) && n.EndsWith(".json")).Order())
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            var file = JsonSerializer.Deserialize<SeedFile>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"Seed dosyası boş: {name}");

            foreach (var question in file.Questions)
            {
                var error = Validate(question);
                if (error is not null)
                {
                    throw new InvalidOperationException($"Geçersiz soru '{question.Key}' ({name}): {error}");
                }
            }

            files.Add(file);
        }

        return files;
    }

    private static Category UpsertCategory(AppDbContext db, Dictionary<string, Category> existing, SeedCategory seed)
    {
        if (!existing.TryGetValue(seed.Id, out var category))
        {
            category = new Category { Id = seed.Id, Title = seed.Title, Description = seed.Description, Icon = seed.Icon };
            db.Categories.Add(category);
            existing[seed.Id] = category;
        }

        category.Title = seed.Title;
        category.Description = seed.Description;
        category.Icon = seed.Icon;
        category.SortOrder = seed.SortOrder;
        return category;
    }

    private static void UpsertQuestion(AppDbContext db, Dictionary<string, Question> existing, string categoryId, SeedQuestion seed)
    {
        if (!existing.TryGetValue(seed.Key, out var question))
        {
            question = new Question
            {
                Key = seed.Key,
                CategoryId = categoryId,
                Prompt = seed.Prompt,
                Explanation = seed.Explanation,
            };
            db.Questions.Add(question);
            existing[seed.Key] = question;
        }

        question.CategoryId = categoryId;
        question.Difficulty = seed.Difficulty;
        question.Type = seed.Type;
        question.Prompt = seed.Prompt;
        question.Code = seed.Code is null ? null : string.Join('\n', seed.Code);
        question.Options = seed.Options ?? [];
        question.CorrectOption = seed.CorrectOption;
        question.CorrectLine = seed.CorrectLine;
        question.Explanation = seed.Explanation;
    }

    private static string? Validate(SeedQuestion q) => q.Type switch
    {
        QuestionType.MultipleChoice when q.Options is not { Count: >= 2 } => "En az 2 şık gerekli.",
        QuestionType.MultipleChoice when q.CorrectOption is not { } o || o < 0 || o >= q.Options!.Count => "correctOption şıkların dışında.",
        QuestionType.BugHunt when q.Code is not { Count: >= 2 } => "En az 2 satır kod gerekli.",
        QuestionType.BugHunt when q.CorrectLine is not { } l || l < 1 || l > q.Code!.Count => "correctLine kodun dışında.",
        QuestionType.Ordering when q.Options is not { Count: >= 3 } => "Sıralama için en az 3 öğe gerekli.",
        QuestionType.Ordering when q.Options!.Distinct().Count() != q.Options.Count => "Sıralama öğeleri benzersiz olmalı.",
        _ when !Enum.IsDefined(q.Difficulty) => "Geçersiz zorluk.",
        _ => null,
    };
}

public sealed record SeedFile(SeedCategory Category, List<SeedQuestion> Questions);

public sealed record SeedCategory(string Id, string Title, string Description, string Icon, int SortOrder);

public sealed record SeedQuestion(
    string Key,
    Difficulty Difficulty,
    QuestionType Type,
    string Prompt,
    List<string>? Code,
    List<string>? Options,
    int? CorrectOption,
    int? CorrectLine,
    string Explanation);
