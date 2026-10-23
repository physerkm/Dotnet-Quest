namespace DotnetQuest.Api.Domain;

/// <param name="Level">1'den başlayan seviye.</param>
/// <param name="Title">Seviye unvanı.</param>
/// <param name="LevelStartXp">Bu seviyeye ulaşmak için gereken XP.</param>
/// <param name="NextLevelXp">Sonraki seviye için gereken XP. Son seviyede null.</param>
public sealed record PlayerLevel(int Level, string Title, int LevelStartXp, int? NextLevelXp)
{
    private static readonly (int Xp, string Title)[] Levels =
    [
        (0, "Stajyer"),
        (50, "Junior Developer"),
        (150, "Junior Developer II"),
        (300, "Mid-Level Developer"),
        (500, "Mid-Level Developer II"),
        (800, "Senior Developer"),
        (1200, "Senior Developer II"),
        (1700, "Tech Lead"),
        (2300, "Software Architect"),
        (3000, ".NET Efsanesi"),
    ];

    public static PlayerLevel FromXp(int xp)
    {
        var index = Math.Max(Array.FindLastIndex(Levels, level => xp >= level.Xp), 0);
        int? next = index + 1 < Levels.Length ? Levels[index + 1].Xp : null;

        return new PlayerLevel(index + 1, Levels[index].Title, Levels[index].Xp, next);
    }
}
