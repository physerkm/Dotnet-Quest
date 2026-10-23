namespace DotnetQuest.Api.Domain;

public enum Difficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
}

public enum QuestionType
{
    /// <summary>Şıklardan birini seç.</summary>
    MultipleChoice = 1,

    /// <summary>Kod parçasındaki hatalı satırı bul.</summary>
    BugHunt = 2,

    /// <summary>Öğeleri doğru sıraya diz.</summary>
    Ordering = 3,
}

public static class DifficultyExtensions
{
    public static int Points(this Difficulty difficulty) => difficulty switch
    {
        Difficulty.Easy => 10,
        Difficulty.Medium => 20,
        Difficulty.Hard => 30,
        _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null),
    };
}
