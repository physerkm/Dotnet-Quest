namespace DotnetQuest.Api.Domain;

public sealed class Question
{
    public int Id { get; private set; }

    /// <summary>Seed dosyasındaki sabit anahtar (ör. "http-easy-01"). İçerik güncellemeleri bu anahtarla eşleşir.</summary>
    public required string Key { get; init; }

    public required string CategoryId { get; set; }
    public Category Category { get; private set; } = null!;

    public Difficulty Difficulty { get; set; }
    public QuestionType Type { get; set; }
    public required string Prompt { get; set; }

    /// <summary>BugHunt sorularında gösterilen kod. Satırlar '\n' ile ayrılır.</summary>
    public string? Code { get; set; }

    /// <summary>
    /// MultipleChoice: şıklar. Ordering: öğeler DOĞRU sırada saklanır, oyuncuya karıştırılarak gönderilir.
    /// </summary>
    public List<string> Options { get; set; } = [];

    /// <summary>MultipleChoice için doğru şıkkın (0 tabanlı) indeksi.</summary>
    public int? CorrectOption { get; set; }

    /// <summary>BugHunt için hatalı satır numarası (1 tabanlı).</summary>
    public int? CorrectLine { get; set; }

    public required string Explanation { get; set; }

    public int CodeLineCount => Code is null ? 0 : Code.Split('\n').Length;
}
