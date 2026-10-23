using DotnetQuest.Api.Data;
using DotnetQuest.Api.Domain;
using DotnetQuest.Api.Features.Sessions;

namespace DotnetQuest.Api.Tests;

public sealed class PlayerLevelTests
{
    [Theory]
    [InlineData(0, 1, "Stajyer")]
    [InlineData(49, 1, "Stajyer")]
    [InlineData(50, 2, "Junior Developer")]
    [InlineData(799, 5, "Mid-Level Developer II")]
    [InlineData(99999, 10, ".NET Efsanesi")]
    public void FromXp_ReturnsExpectedLevel(int xp, int expectedLevel, string expectedTitle)
    {
        var level = PlayerLevel.FromXp(xp);

        Assert.Equal(expectedLevel, level.Level);
        Assert.Equal(expectedTitle, level.Title);
    }

    [Fact]
    public void FromXp_AtMaxLevel_HasNoNextLevel()
    {
        Assert.Null(PlayerLevel.FromXp(5000).NextLevelXp);
    }
}

public sealed class GameSessionTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RecordAnswer_LastQuestion_CompletesSession()
    {
        var session = GameSession.Start("http", Difficulty.Hard, "player-1", [1, 2], Now);

        session.RecordAnswer(1, isCorrect: true, Now);
        Assert.False(session.IsCompleted);

        session.RecordAnswer(2, isCorrect: false, Now);
        Assert.True(session.IsCompleted);
        Assert.Equal(30, session.Score);
        Assert.False(session.IsPerfect);
        Assert.Equal(30, session.CalculateXpReward());
    }

    [Fact]
    public void PerfectSession_GetsTwentyFivePercentBonus()
    {
        var session = GameSession.Start("http", Difficulty.Medium, null, [1, 2, 3, 4], Now);

        foreach (var id in new[] { 1, 2, 3, 4 })
        {
            session.RecordAnswer(id, isCorrect: true, Now);
        }

        Assert.Equal(80, session.Score);
        Assert.Equal(100, session.CalculateXpReward());
    }

    [Fact]
    public void RecordAnswer_SameQuestionTwice_Throws()
    {
        var session = GameSession.Start("http", Difficulty.Easy, null, [1, 2], Now);
        session.RecordAnswer(1, isCorrect: true, Now);

        Assert.Throws<InvalidOperationException>(() => session.RecordAnswer(1, isCorrect: true, Now));
    }
}

public sealed class OptionShufflerTests
{
    [Fact]
    public void GetPermutation_IsDeterministicForSameSessionAndQuestion()
    {
        var sessionId = Guid.NewGuid();

        var first = OptionShuffler.GetPermutation(sessionId, 7, 4, avoidOriginalOrder: false);
        var second = OptionShuffler.GetPermutation(sessionId, 7, 4, avoidOriginalOrder: false);

        Assert.Equal(first, second);
        Assert.Equal([0, 1, 2, 3], first.Order());
    }

    [Fact]
    public void GetPermutation_ForOrdering_NeverReturnsOriginalOrder()
    {
        for (var i = 0; i < 500; i++)
        {
            var permutation = OptionShuffler.GetPermutation(Guid.NewGuid(), i, 3, avoidOriginalOrder: true);
            Assert.NotEqual([0, 1, 2], permutation);
        }
    }
}

public sealed class SeedContentTests
{
    [Fact]
    public void SeedFiles_AreValid_AndKeysAreUnique()
    {
        var files = ContentSeeder.LoadSeedFiles();
        var keys = files.SelectMany(f => f.Questions).Select(q => q.Key).ToList();

        Assert.NotEmpty(files);
        Assert.Equal(keys.Count, keys.Distinct().Count());
    }

    [Fact]
    public void EveryCategory_HasEnoughQuestionsForAFullGameAtEveryDifficulty()
    {
        foreach (var file in ContentSeeder.LoadSeedFiles())
        {
            foreach (var difficulty in Enum.GetValues<Difficulty>())
            {
                var count = file.Questions.Count(q => q.Difficulty == difficulty);
                Assert.True(count >= GameSession.QuestionCount, $"{file.Category.Id}/{difficulty}: {count} soru");
            }
        }
    }
}
