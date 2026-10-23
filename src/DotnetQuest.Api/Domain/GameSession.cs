namespace DotnetQuest.Api.Domain;

/// <summary>
/// Bir oyun turu. İş kuralları (puanlama, tamamlanma, XP) burada yaşar; endpoint'ler sadece orkestrasyon yapar.
/// </summary>
public sealed class GameSession
{
    public const int QuestionCount = 5;

    private GameSession() { } // EF Core için

    public Guid Id { get; private set; }
    public string? PlayerId { get; private set; }
    public string CategoryId { get; private set; } = null!;
    public Category Category { get; private set; } = null!;
    public Difficulty Difficulty { get; private set; }
    public DateTime StartedAtUtc { get; private set; }
    public DateTime? CompletedAtUtc { get; private set; }
    public int Score { get; private set; }

    /// <summary>Optimistic concurrency token: aynı cevabın iki kez sayılmasını engeller.</summary>
    public int Version { get; private set; }

    public List<SessionQuestion> Questions { get; private set; } = [];

    public bool IsCompleted => CompletedAtUtc is not null;
    public int MaxScore => Questions.Count * Difficulty.Points();
    public bool IsPerfect => IsCompleted && Questions.All(q => q.IsCorrect == true);

    public static GameSession Start(
        string categoryId,
        Difficulty difficulty,
        string? playerId,
        IReadOnlyList<int> questionIds,
        DateTime nowUtc)
    {
        if (questionIds.Count == 0)
        {
            throw new ArgumentException("Bir oyun en az bir soru içermelidir.", nameof(questionIds));
        }

        // Version 7 GUID'ler zamana göre sıralıdır; index'lerde rastgele GUID'e göre daha az parçalanma olur.
        var id = Guid.CreateVersion7();

        return new GameSession
        {
            Id = id,
            CategoryId = categoryId,
            Difficulty = difficulty,
            PlayerId = playerId,
            StartedAtUtc = nowUtc,
            Questions = questionIds.Select((questionId, index) => new SessionQuestion(id, questionId, index + 1)).ToList(),
        };
    }

    public void RecordAnswer(int questionId, bool isCorrect, DateTime nowUtc)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Tamamlanmış bir oyuna cevap eklenemez.");
        }

        var question = Questions.Single(q => q.QuestionId == questionId);
        question.MarkAnswered(isCorrect, isCorrect ? Difficulty.Points() : 0, nowUtc);

        Score += question.PointsEarned;
        Version++;

        if (Questions.All(q => q.IsAnswered))
        {
            CompletedAtUtc = nowUtc;
        }
    }

    /// <summary>Tamamlanan tur için kazanılan XP. Hatasız turlara %25 bonus verilir.</summary>
    public int CalculateXpReward()
    {
        if (!IsCompleted)
        {
            return 0;
        }

        return Score + (IsPerfect ? Score / 4 : 0);
    }
}

public sealed class SessionQuestion
{
    private SessionQuestion() { } // EF Core için

    internal SessionQuestion(Guid sessionId, int questionId, int order)
    {
        SessionId = sessionId;
        QuestionId = questionId;
        Order = order;
    }

    public Guid SessionId { get; private set; }
    public int QuestionId { get; private set; }
    public Question Question { get; private set; } = null!;
    public int Order { get; private set; }
    public bool? IsCorrect { get; private set; }
    public int PointsEarned { get; private set; }
    public DateTime? AnsweredAtUtc { get; private set; }

    public bool IsAnswered => AnsweredAtUtc is not null;

    internal void MarkAnswered(bool isCorrect, int points, DateTime nowUtc)
    {
        if (IsAnswered)
        {
            throw new InvalidOperationException("Bu soru zaten cevaplandı.");
        }

        IsCorrect = isCorrect;
        PointsEarned = points;
        AnsweredAtUtc = nowUtc;
    }
}
